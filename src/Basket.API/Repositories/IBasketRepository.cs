using eShop.Basket.API.Model;

namespace eShop.Basket.API.Repositories;

/// <summary>
/// 购物车仓储接口，定义购物车数据的持久化（读取、更新、删除）契约。
/// </summary>
public interface IBasketRepository
{
    /// <summary>
    /// 获取指定买家 ID 的购物车数据
    /// </summary>
    Task<CustomerBasket?> GetBasketAsync(string customerId);

    /// <summary>
    /// 更新或新建买家购物车
    /// </summary>
    Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket basket);

    /// <summary>
    /// 删除指定买家 ID 的购物车
    /// </summary>
    Task<bool> DeleteBasketAsync(string id);
}
