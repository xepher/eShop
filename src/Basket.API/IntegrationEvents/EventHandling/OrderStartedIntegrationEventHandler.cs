using eShop.Basket.API.Repositories;
using eShop.Basket.API.IntegrationEvents.EventHandling.Events;

namespace eShop.Basket.API.IntegrationEvents.EventHandling;

/// <summary>
/// 订单启动集成事件处理器。
/// 当用户发起下单流程并成功开始订单处理时，会广播 OrderStartedIntegrationEvent，
/// 该处理器会拦截并自动清空该用户的临时购物车，保证购物车生命周期的闭环。
/// </summary>
public class OrderStartedIntegrationEventHandler(
    IBasketRepository repository,
    ILogger<OrderStartedIntegrationEventHandler> logger) : IIntegrationEventHandler<OrderStartedIntegrationEvent>
{
    /// <summary>
    /// 处理订单启动事件，清空当前用户的购物车缓存
    /// </summary>
    public async Task Handle(OrderStartedIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        // 调用购物车仓储异步删除对应用户的购物车
        await repository.DeleteBasketAsync(@event.UserId);
    }
}
