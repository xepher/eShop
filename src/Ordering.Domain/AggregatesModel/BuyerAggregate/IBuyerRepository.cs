namespace eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;

// 这是在领域层定义的仓储契约或接口，
// 作为买家聚合的必要依赖。

/// <summary>
/// 买家聚合的仓储接口。定义了买家数据的持久化与查询契约。
/// </summary>
public interface IBuyerRepository : IRepository<Buyer>
{
    /// <summary>
    /// 添加新买家。
    /// </summary>
    /// <param name="buyer">买家聚合实例。</param>
    /// <returns>已添加的买家实例。</returns>
    Buyer Add(Buyer buyer);

    /// <summary>
    /// 更新现有买家。
    /// </summary>
    /// <param name="buyer">买家聚合实例。</param>
    /// <returns>已更新的买家实例。</returns>
    Buyer Update(Buyer buyer);

    /// <summary>
    /// 根据买家的身份标识（IdentityGuid）异步查找买家。
    /// </summary>
    /// <param name="BuyerIdentityGuid">身份提供程序的买家唯一标识。</param>
    /// <returns>买家实例，如果未找到则返回 null。</returns>
    Task<Buyer> FindAsync(string BuyerIdentityGuid);

    /// <summary>
    /// 根据买家在数据库中的主键 Id 异步查找买家。
    /// </summary>
    /// <param name="id">买家内部主键 ID。</param>
    /// <returns>买家实例，如果未找到则返回 null。</returns>
    Task<Buyer> FindByIdAsync(int id);
}

