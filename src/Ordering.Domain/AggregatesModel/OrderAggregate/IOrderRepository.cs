namespace eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

// 这是在领域层定义的仓储契约或接口，
// 作为订单聚合的必要依赖。

/// <summary>
/// 订单聚合的仓储接口。定义了订单数据的持久化与查询契约。
/// </summary>
public interface IOrderRepository : IRepository<Order>
{
    /// <summary>
    /// 添加新订单。
    /// </summary>
    /// <param name="order">订单聚合实例。</param>
    /// <returns>已添加的订单实例。</returns>
    Order Add(Order order);

    /// <summary>
    /// 更新现有订单。
    /// </summary>
    /// <param name="order">订单聚合实例。</param>
    void Update(Order order);

    /// <summary>
    /// 根据订单主键 ID 异步获取订单。
    /// </summary>
    /// <param name="orderId">订单唯一标识。</param>
    /// <returns>订单聚合根，如果未找到则返回 null。</returns>
    Task<Order> GetAsync(int orderId);
}
