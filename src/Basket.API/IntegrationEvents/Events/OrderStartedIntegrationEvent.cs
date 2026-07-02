namespace eShop.Basket.API.IntegrationEvents.EventHandling.Events;

/// <summary>
/// 订单已启动集成事件。
/// 集成事件（Integration Event）用于实现跨微服务边界（Bounded-Contexts）或外部系统之间的最终一致性。
/// 命名规则通常为过去时态，代表系统里已经发生的事情。
/// </summary>
/// <param name="UserId">下达订单的买家唯一用户标识符</param>
public record OrderStartedIntegrationEvent(string UserId) : IntegrationEvent;
