using System.Text.Json.Serialization;

namespace eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

/// <summary>
/// 订单状态枚举。定义了订单在其生命周期中所处的所有阶段。
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    /// <summary>
    /// 已提交：订单已被买家创建并提交，初始默认状态。
    /// </summary>
    Submitted = 1,

    /// <summary>
    /// 等待验证：正在等待库存或买家支付方式验证。
    /// </summary>
    AwaitingValidation = 2,

    /// <summary>
    /// 库存已确认：订单所需的商品库存已经成功锁定/确认。
    /// </summary>
    StockConfirmed = 3,

    /// <summary>
    /// 已付款：买家已成功支付该订单。
    /// </summary>
    Paid = 4,

    /// <summary>
    /// 已发货：订单对应的商品已被发出。
    /// </summary>
    Shipped = 5,

    /// <summary>
    /// 已取消：订单已被取消。
    /// </summary>
    Cancelled = 6
}
