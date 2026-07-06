namespace eShop.EventBus.Abstractions;

/// <summary>
/// 泛型集成事件处理器接口。
/// 用于处理特定类型的集成事件。
/// </summary>
/// <typeparam name="TIntegrationEvent">要处理的集成事件的类型，必须继承自 <see cref="IntegrationEvent"/>。</typeparam>
public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
    where TIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// 异步处理特定类型的集成事件。
    /// </summary>
    /// <param name="event">接收到的集成事件实例。</param>
    /// <returns>表示异步处理操作的 Task。</returns>
    Task Handle(TIntegrationEvent @event);

    /// <summary>
    /// 显式实现非泛型的 Handle 方法，将其转换为强类型的泛型 Handle 调用。
    /// </summary>
    /// <param name="event">接收到的集成事件实例。</param>
    /// <returns>表示异步处理操作的 Task。</returns>
    Task IIntegrationEventHandler.Handle(IntegrationEvent @event) => Handle((TIntegrationEvent)@event);
}

/// <summary>
/// 非泛型集成事件处理器基接口。
/// 用于事件总线在不感知具体事件强类型时统一调用 Handle 操作。
/// </summary>
public interface IIntegrationEventHandler
{
    /// <summary>
    /// 异步处理集成事件。
    /// </summary>
    /// <param name="event">接收到的集成事件实例。</param>
    /// <returns>表示异步处理操作的 Task。</returns>
    Task Handle(IntegrationEvent @event);
}

