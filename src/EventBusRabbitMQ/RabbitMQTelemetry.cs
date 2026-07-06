using System.Diagnostics;
using OpenTelemetry.Context.Propagation;

namespace eShop.EventBusRabbitMQ;

/// <summary>
/// RabbitMQ 事件总线遥测类，集成 OpenTelemetry 进行分布式追踪。
/// </summary>
public class RabbitMQTelemetry
{
    /// <summary>
    /// 获取或设置追踪活动源（ActivitySource）的名称。
    /// </summary>
    public static string ActivitySourceName = "EventBusRabbitMQ";

    /// <summary>
    /// 获取当前组件的 OpenTelemetry <see cref="ActivitySource"/> 追踪源实例。
    /// </summary>
    public ActivitySource ActivitySource { get; } = new(ActivitySourceName);

    /// <summary>
    /// 获取 OpenTelemetry 分布式跟踪上下文传播器（Propagator），用于在消息属性中读写追踪上下文。
    /// </summary>
    public TextMapPropagator Propagator { get; } = Propagators.DefaultTextMapPropagator;
}

