using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using eShop.EventBus.Abstractions;
using eShop.EventBus.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 事件总线构建器的扩展方法类，用于配置 JSON 序列化选项和注册事件订阅。
/// </summary>
public static class EventBusBuilderExtensions
{
    /// <summary>
    /// 配置事件序列化/反序列化时使用的 JSON 配置选项。
    /// </summary>
    /// <param name="eventBusBuilder">当前事件总线构建器实例。</param>
    /// <param name="configure">用于配置 <see cref="JsonSerializerOptions"/> 的委托。</param>
    /// <returns>返回事件总线构建器以支持链式调用。</returns>
    public static IEventBusBuilder ConfigureJsonOptions(this IEventBusBuilder eventBusBuilder, Action<JsonSerializerOptions> configure)
    {
        eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
        {
            configure(o.JsonSerializerOptions);
        });

        return eventBusBuilder;
    }

    /// <summary>
    /// 向事件总线注册一个集成事件订阅及其对应的事件处理器。
    /// </summary>
    /// <typeparam name="T">要订阅的集成事件类型，必须继承自 <see cref="IntegrationEvent"/>。</typeparam>
    /// <typeparam name="TH">处理该事件的事件处理器类型，必须实现 <see cref="IIntegrationEventHandler{T}"/>。</typeparam>
    /// <param name="eventBusBuilder">当前事件总线构建器实例。</param>
    /// <returns>返回事件总线构建器以支持链式调用。</returns>
    public static IEventBusBuilder AddSubscription<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TH>(this IEventBusBuilder eventBusBuilder)
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        // Use keyed services to register multiple handlers for the same event type
        // the consumer can use IKeyedServiceProvider.GetKeyedService<IIntegrationEventHandler>(typeof(T)) to get all
        // handlers for the event type.
        // 使用有键服务（Keyed Services）注册同一事件类型的多个处理器。
        // 消费者可以使用 IKeyedServiceProvider.GetKeyedServices<IIntegrationEventHandler>(typeof(T)) 来获取该事件类型的所有处理器。
        eventBusBuilder.Services.AddKeyedTransient<IIntegrationEventHandler, TH>(typeof(T));

        eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
        {
            // Keep track of all registered event types and their name mapping. We send these event types over the message bus
            // and we don't want to do Type.GetType, so we keep track of the name mapping here.

            // This list will also be used to subscribe to events from the underlying message broker implementation.
            
            // 跟踪所有已注册的事件类型及其名称映射。我们在消息总线上传输这些事件类型，
            // 并且不希望使用 Type.GetType，因此在这里跟踪它们的名称映射关系。
            // 此列表也将用于在底层的消息代理（如 RabbitMQ）中订阅事件。
            o.EventTypes[typeof(T).Name] = typeof(T);
        });

        return eventBusBuilder;
    }
}

