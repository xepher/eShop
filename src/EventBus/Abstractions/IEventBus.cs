namespace eShop.EventBus.Abstractions;

/// <summary>
/// 事件总线接口，用于在不同的微服务（Bounded Contexts）之间发布和传递集成事件，实现最终一致性。
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 异步发布一个集成事件到事件总线（如 RabbitMQ 等中间件）。
    /// </summary>
    /// <param name="event">要发布的集成事件实例。</param>
    /// <returns>表示异步发布操作的 Task。</returns>
    Task PublishAsync(IntegrationEvent @event);
}

