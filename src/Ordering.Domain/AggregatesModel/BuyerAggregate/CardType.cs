namespace eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;

/// <summary>
/// 卡片类型。表示支付方式支持的银行卡种类（例如：Visa、MasterCard 等）。
/// </summary>
public sealed class CardType
{
    /// <summary>
    /// 卡片类型标识。
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 卡片类型名称（如 "Visa", "MasterCard"）。
    /// </summary>
    public required string Name { get; init; }
}
