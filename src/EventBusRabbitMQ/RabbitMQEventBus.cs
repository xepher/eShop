namespace eShop.EventBusRabbitMQ;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Polly.Retry;

/// <summary>
/// 基于 RabbitMQ 的事件总线实现类。
/// 实现了 <see cref="IEventBus"/> 接口，支持将集成事件发布到 RabbitMQ；
/// 实现了 <see cref="IHostedService"/> 接口，支持作为后台服务在程序启动时自动开启对订阅事件的消费监听。
/// </summary>
public sealed class RabbitMQEventBus(
    ILogger<RabbitMQEventBus> logger,
    IServiceProvider serviceProvider,
    IOptions<EventBusOptions> options,
    IOptions<EventBusSubscriptionInfo> subscriptionOptions,
    RabbitMQTelemetry rabbitMQTelemetry) : IEventBus, IDisposable, IHostedService
{
    /// <summary>
    /// 事件总线使用的 RabbitMQ 交换机（Exchange）名称。
    /// </summary>
    private const string ExchangeName = "eshop_event_bus";

    /// <summary>
    /// 异常恢复重试弹性策略流水线（用于解决发布或消费时暂时的网络抖动、Broker 不可用等问题）。
    /// </summary>
    private readonly ResiliencePipeline _pipeline = CreateResiliencePipeline(options.Value.RetryCount);

    /// <summary>
    /// OpenTelemetry 的追踪上下文传播器，用于在发布/接收消息时传递 Span 等追踪数据。
    /// </summary>
    private readonly TextMapPropagator _propagator = rabbitMQTelemetry.Propagator;

    /// <summary>
    /// OpenTelemetry 追踪源实例，用于启动相关的 Span。
    /// </summary>
    private readonly ActivitySource _activitySource = rabbitMQTelemetry.ActivitySource;

    /// <summary>
    /// 当前微服务的订阅队列名称。
    /// </summary>
    private readonly string _queueName = options.Value.SubscriptionClientName;

    /// <summary>
    /// 已注册的集成事件及其处理器订阅信息。
    /// </summary>
    private readonly EventBusSubscriptionInfo _subscriptionInfo = subscriptionOptions.Value;

    /// <summary>
    /// 底层的 RabbitMQ 连接实例。
    /// </summary>
    private IConnection _rabbitMQConnection;

    /// <summary>
    /// 持续消费消息的 RabbitMQ 通道（Channel）实例。
    /// </summary>
    private IChannel _consumerChannel;

    /// <summary>
    /// 异步发布一个集成事件到 RabbitMQ 中。
    /// </summary>
    /// <param name="event">要发布的集成事件实例。</param>
    public async Task PublishAsync(IntegrationEvent @event)
    {
        var routingKey = @event.GetType().Name;

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id, routingKey);
        }

        // 创建临时通道来发布消息
        using var channel = (await _rabbitMQConnection?.CreateChannelAsync()) ?? throw new InvalidOperationException("RabbitMQ connection is not open");

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);
        }

        // 声明直连（Direct）交换机
        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName, 
            type: "direct");

        var body = SerializeMessage(@event);

        // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md
        // 根据 OpenTelemetry 语义约定启动一个发布追踪活动
        var activityName = $"{routingKey} publish";

        await _pipeline.Execute(async () =>
        {
            using var activity = _activitySource.StartActivity(activityName, ActivityKind.Client);

            // Depending on Sampling (and whether a listener is registered or not), the activity above may not be created.
            // If it is created, then propagate its context. If it is not created, the propagate the Current context, if any.
            // 依赖于采样策略，上面创建的 activity 可能为 null。如果是，则尝试传播当前活动上下文。
            ActivityContext contextToInject = default;

            if (activity != null)
            {
                contextToInject = activity.Context;
            }
            else if (Activity.Current != null)
            {
                contextToInject = Activity.Current.Context;
            }

            var properties = new BasicProperties()
            {
                DeliveryMode = DeliveryModes.Persistent // 消息持久化投递模式
            };

            // 辅助本地函数：将追踪上下文注入到 BasicProperties.Headers 中
            static void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
            {
                props.Headers ??= new Dictionary<string, object>();
                props.Headers[key] = value;
            }

            _propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), properties, InjectTraceContextIntoBasicProperties);

            // 设置追踪标签
            SetActivityContext(activity, routingKey, "publish");

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace("Publishing event to RabbitMQ: {EventId}", @event.Id);
            }

            try
            {
                // 发布消息
                await channel.BasicPublishAsync(
                    exchange: ExchangeName,
                    routingKey: routingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            }
            catch (Exception ex)
            {
                activity.SetExceptionTags(ex);
                throw;
            }
        });
    }

    /// <summary>
    /// 设置 OpenTelemetry 活动（Activity）的语义标签。
    /// </summary>
    private static void SetActivityContext(Activity activity, string routingKey, string operation)
    {
        if (activity is not null)
        {
            // These tags are added demonstrating the semantic conventions of the OpenTelemetry messaging specification
            // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md
            activity.SetTag("messaging.system", "rabbitmq");
            activity.SetTag("messaging.destination_kind", "queue");
            activity.SetTag("messaging.operation", operation);
            activity.SetTag("messaging.destination.name", routingKey);
            activity.SetTag("messaging.rabbitmq.routing_key", routingKey);
        }
    }

    /// <summary>
    /// 释放消费通道资源。
    /// </summary>
    public void Dispose()
    {
        _consumerChannel?.Dispose();
    }

    /// <summary>
    /// 当从队列中接收到消息时的异步回调处理方法。
    /// </summary>
    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        // 辅助本地函数：从消息头部提取追踪上下文
        static IEnumerable<string> ExtractTraceContextFromBasicProperties(IReadOnlyBasicProperties props, string key)
        {
            if (props.Headers.TryGetValue(key, out var value))
            {
                var bytes = value as byte[];
                return [Encoding.UTF8.GetString(bytes)];
            }
            return [];
        }

        // Extract the PropagationContext of the upstream parent from the message headers.
        // 从消息头提取上游父级的追踪传播上下文
        var parentContext = _propagator.Extract(default, eventArgs.BasicProperties, ExtractTraceContextFromBasicProperties);
        Baggage.Current = parentContext.Baggage;

        // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
        // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md
        // 根据 OpenTelemetry 语义约定启动一个消息接收追踪活动
        var activityName = $"{eventArgs.RoutingKey} receive";

        using var activity = _activitySource.StartActivity(activityName, ActivityKind.Client, parentContext.ActivityContext);

        SetActivityContext(activity, eventArgs.RoutingKey, "receive");

        var eventName = eventArgs.RoutingKey;
        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

        try
        {
            activity?.SetTag("message", message);

            // 如果消息中包含测试抛异常的标志，则抛出模拟异常
            if (message.Contains("throw-fake-exception", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
            }

            // 处理接收到的事件并分发给具体处理器
            await ProcessEvent(eventName, message);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error Processing message \"{Message}\"", message);

            activity.SetExceptionTags(ex);
        }

        // Even on exception we take the message off the queue.
        // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
        // For more information see: https://www.rabbitmq.com/dlx.html
        // 即使处理中发生异常，我们也应做 ACK 响应来确认并从当前队列移除消息。
        // 在生产环境中，应当配合死信队列（Dead Letter Exchange）等进行容错和归档。
        await _consumerChannel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
    }

    /// <summary>
    /// 解析 JSON 并通过有键服务（Keyed Services）获取相应处理者进行事件处理分发。
    /// </summary>
    private async Task ProcessEvent(string eventName, string message)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);
        }

        // 创建临时异步范围生命周期，以便为处理器注入所需的瞬时/范围依赖服务
        await using var scope = serviceProvider.CreateAsyncScope();

        // 尝试从订阅的映射表中寻找匹配的运行时事件类型
        if (!_subscriptionInfo.EventTypes.TryGetValue(eventName, out var eventType))
        {
            logger.LogWarning("Unable to resolve event type for event name {EventName}", eventName);
            return;
        }

        // 反序列化为具体的集成事件
        var integrationEvent = DeserializeMessage(message, eventType);
        
        // REVIEW: This could be done in parallel
        // 获取所有与此事件类型（作为 Key）绑定的处理器并执行
        foreach (var handler in scope.ServiceProvider.GetKeyedServices<IIntegrationEventHandler>(eventType))
        {
            await handler.Handle(integrationEvent);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "The 'JsonSerializer.IsReflectionEnabledByDefault' feature switch, which is set to false by default for trimmed .NET apps, ensures the JsonSerializer doesn't use Reflection.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "See above.")]
    private IntegrationEvent DeserializeMessage(string message, Type eventType)
    {
        return JsonSerializer.Deserialize(message, eventType, _subscriptionInfo.JsonSerializerOptions) as IntegrationEvent;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "The 'JsonSerializer.IsReflectionEnabledByDefault' feature switch, which is set to false by default for trimmed .NET apps, ensures the JsonSerializer doesn't use Reflection.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "See above.")]
    private byte[] SerializeMessage(IntegrationEvent @event)
    {
        return JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), _subscriptionInfo.JsonSerializerOptions);
    }

    /// <summary>
    /// 托管服务生命周期：在后台线程上异步初始化 RabbitMQ 连接，声明交换机、绑定队列，并启动异步消息消费监听。
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Messaging is async so we don't need to wait for it to complete.
        // 消息启动是非阻塞异步的过程，所以另起一个长期任务线程初始化
        _ = Task.Factory.StartNew(async () =>
        {
            try
            {
                logger.LogInformation("Starting RabbitMQ connection on a background thread");

                _rabbitMQConnection = serviceProvider.GetRequiredService<IConnection>();
                if (!_rabbitMQConnection.IsOpen)
                {
                    return;
                }

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Creating RabbitMQ consumer channel");
                }

                // 创建消费者通道
                _consumerChannel = await _rabbitMQConnection.CreateChannelAsync();

                // 捕获通道上的回调异常并记录日志
                _consumerChannel.CallbackExceptionAsync += (sender, ea) =>
                {
                    logger.LogWarning(ea.Exception, "Error with RabbitMQ consumer channel");
                    return Task.CompletedTask;
                };

                // 声明事件交换机
                await _consumerChannel.ExchangeDeclareAsync(
                    exchange: ExchangeName,
                    type: "direct");

                // 声明消费者的持久化队列
                await _consumerChannel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Starting RabbitMQ basic consume");
                }

                // 创建异步消费者
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                // 绑定消息接收回调事件
                consumer.ReceivedAsync += OnMessageReceived;

                // 启动 Basic 消费
                await _consumerChannel.BasicConsumeAsync(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer);

                // 根据所有注册过的集成事件类型，循环将其类型名称绑定到对应的交换机和队列
                foreach (var (eventName, _) in _subscriptionInfo.EventTypes)
                {
                    await _consumerChannel.QueueBindAsync(
                        queue: _queueName,
                        exchange: ExchangeName,
                        routingKey: eventName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error starting RabbitMQ connection");
            }
        },
        TaskCreationOptions.LongRunning);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 托管服务生命周期：在应用程序停止时执行。
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 创建错误重试策略弹性管道（支持针对 BrokerUnreachableException 和 SocketException 进行指数回退）。
    /// </summary>
    private static ResiliencePipeline CreateResiliencePipeline(int retryCount)
    {
        // See https://www.pollydocs.org/strategies/retry.html
        var retryOptions = new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>().Handle<SocketException>(),
            MaxRetryAttempts = retryCount,
            DelayGenerator = (context) => ValueTask.FromResult(GenerateDelay(context.AttemptNumber))
        };

        return new ResiliencePipelineBuilder()
            .AddRetry(retryOptions)
            .Build();

        static TimeSpan? GenerateDelay(int attempt)
        {
            return TimeSpan.FromSeconds(Math.Pow(2, attempt));
        }
    }
}

