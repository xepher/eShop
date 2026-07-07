using System.ComponentModel.DataAnnotations;

namespace eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;

/// <summary>
/// 买家聚合根。表示系统中的客户，并维护该客户相关的支付方式列表。
/// </summary>
public class Buyer
    : Entity, IAggregateRoot
{
    /// <summary>
    /// 买家的身份唯一标识（通常关联自外部标识提供程序 IDP）。
    /// </summary>
    [Required]
    public string IdentityGuid { get; private set; }

    /// <summary>
    /// 买家的名称。
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// 买家绑定的支付方式内部列表。
    /// </summary>
    private List<PaymentMethod> _paymentMethods;

    /// <summary>
    /// 提供只读访问的支付方式集合，确保集合封装性，防止从外部直接修改。
    /// </summary>
    public IEnumerable<PaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

    /// <summary>
    /// 用于 ORM (EF Core) 反序列化或派生的受保护无参构造函数。
    /// </summary>
    protected Buyer()
    {
        _paymentMethods = new List<PaymentMethod>();
    }

    /// <summary>
    /// 创建新买家的构造函数。
    /// </summary>
    /// <param name="identity">买家的身份标识，不能为空。</param>
    /// <param name="name">买家名称，不能为空。</param>
    /// <exception cref="ArgumentNullException">当身份标识或名称为空时抛出。</exception>
    public Buyer(string identity, string name) : this()
    {
        // 验证约束：买家身份标识不能为空
        IdentityGuid = !string.IsNullOrWhiteSpace(identity) ? identity : throw new ArgumentNullException(nameof(identity));
        // 验证约束：买家名称不能为空
        Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// 验证并获取已存在的支付方式，如果不存在则添加新的支付方式，并触发领域事件。
    /// </summary>
    /// <param name="cardTypeId">卡片类型标识。</param>
    /// <param name="alias">支付方式别名。</param>
    /// <param name="cardNumber">卡号。</param>
    /// <param name="securityNumber">安全码。</param>
    /// <param name="cardHolderName">持卡人姓名。</param>
    /// <param name="expiration">过期时间。</param>
    /// <param name="orderId">订单唯一标识，用于在验证后关联并处理订单支付。</param>
    /// <returns>验证通过或新建的支付方式实例。</returns>
    public PaymentMethod VerifyOrAddPaymentMethod(
        int cardTypeId, string alias, string cardNumber,
        string securityNumber, string cardHolderName, DateTime expiration, int orderId)
    {
        // 检查买家当前已绑定的支付方式中，是否已存在相同卡类型、卡号和过期时间的支付方式
        var existingPayment = _paymentMethods
            .SingleOrDefault(p => p.IsEqualTo(cardTypeId, cardNumber, expiration));

        if (existingPayment != null)
        {
            // 如果已存在相同的支付方式，则直接触发“买家与支付方式已验证”领域事件，并返回
            AddDomainEvent(new BuyerAndPaymentMethodVerifiedDomainEvent(this, existingPayment, orderId));

            return existingPayment;
        }

        // 如果不存在，则创建新的支付方式实体（约束：卡属性合法性在 PaymentMethod 内部校验）
        var payment = new PaymentMethod(cardTypeId, alias, cardNumber, securityNumber, cardHolderName, expiration);

        _paymentMethods.Add(payment);

        // 触发领域事件以通知其他订阅者（例如：更新订单的支付方式）
        AddDomainEvent(new BuyerAndPaymentMethodVerifiedDomainEvent(this, payment, orderId));

        return payment;
    }
}
