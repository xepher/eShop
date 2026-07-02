namespace eShop.Basket.API.Model;

/// <summary>
/// 顾客购物车聚合根模型，包含买家标识和商品项列表。
/// </summary>
public class CustomerBasket
{
    /// <summary>
    /// 买家唯一标识（对应用户的 OIDC 主体 Subject / User ID）
    /// </summary>
    [Required]
    public string BuyerId { get; set; } = default!;

    /// <summary>
    /// 购物车中所有的商品明细列表
    /// </summary>
    public List<BasketItem> Items { get; set; } = [];

    public CustomerBasket() { }

    public CustomerBasket(string customerId)
    {
        BuyerId = customerId;
    }
}
