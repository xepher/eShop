using eShop.EventBusRabbitMQ;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// 依赖注入扩展类，用于向应用程序主机添加并配置基于 RabbitMQ 的事件总线。
/// </summary>
public static class RabbitMqDependencyInjectionExtensions
{
    // {
    //   "EventBus": {
    //     "SubscriptionClientName": "...",
    //     "RetryCount": 10
    //   }
    // }

    /// <summary>
    /// 配置中事件总线配置节的名称。
    /// </summary>
    private const string SectionName = "EventBus";

    /// <summary>
    /// 向应用程序的宿主构建器注册 RabbitMQ 客户端和 RabbitMQEventBus 服务。
    /// </summary>
    /// <param name="builder">主机的 <see cref="IHostApplicationBuilder"/>。</param>
    /// <param name="connectionName">RabbitMQ 连接的名称（配置在 ConnectionStrings 下）。</param>
    /// <returns>返回事件总线构建器 <see cref="IEventBusBuilder"/>，用于链式配置订阅。</returns>
    public static IEventBusBuilder AddRabbitMqEventBus(this IHostApplicationBuilder builder, string connectionName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // 注册底层的 RabbitMQ.Client 实例（通常通过 Aspire 组件或扩展）
        builder.AddRabbitMQClient(connectionName);

        // RabbitMQ.Client doesn't have built-in support for OpenTelemetry, so we need to add it ourselves
        // RabbitMQ.Client 没有内置的 OpenTelemetry 支持，所以我们需要手动将追踪活动源添加到 OpenTelemetry
        builder.Services.AddOpenTelemetry()
           .WithTracing(tracing =>
           {
               tracing.AddSource(RabbitMQTelemetry.ActivitySourceName);
           });

        // Options support
        // 绑定配置中的 EventBus 节点到 EventBusOptions 选项上
        builder.Services.Configure<EventBusOptions>(builder.Configuration.GetSection(SectionName));

        // Abstractions on top of the core client API
        // 注册遥测辅助服务和具体的事件总线实现类
        builder.Services.AddSingleton<RabbitMQTelemetry>();
        builder.Services.AddSingleton<IEventBus, RabbitMQEventBus>();
        
        // Start consuming messages as soon as the application starts
        // 将 RabbitMQEventBus 同时注册为后台托管服务（IHostedService），以便在应用程序启动时自动开始消费消息
        builder.Services.AddSingleton<IHostedService>(sp => (RabbitMQEventBus)sp.GetRequiredService<IEventBus>());

        return new EventBusBuilder(builder.Services);
    }

    /// <summary>
    /// IEventBusBuilder 的内部默认实现，包装服务集合。
    /// </summary>
    private class EventBusBuilder(IServiceCollection services) : IEventBusBuilder
    {
        public IServiceCollection Services => services;
    }
}

