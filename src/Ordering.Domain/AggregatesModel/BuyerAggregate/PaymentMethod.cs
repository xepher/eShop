using System.ComponentModel.DataAnnotations;

namespace eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;

/// <summary>
/// 支付方式实体。作为买家聚合内部的子实体，表示具体的支付卡（如信用卡/借记卡）信息。
/// </summary>
public class PaymentMethod : Entity
{
    [Required]
    private string _alias;
    
    [Required]
    private string _cardNumber;
    
    private string _securityNumber;
    
    [Required]
    private string _cardHolderName;
    
    private DateTime _expiration;

    private int _cardTypeId;
    
    /// <summary>
    /// 卡片类型导航属性。
    /// </summary>
    public CardType CardType { get; private set; }

    /// <summary>
    /// 用于 ORM (EF Core) 反序列化或映射的受保护无参构造函数。
    /// </summary>
    protected PaymentMethod() { }

    /// <summary>
    /// 创建支付方式实体的构造函数。
    /// </summary>
    /// <param name="cardTypeId">卡片类型标识。</param>
    /// <param name="alias">支付方式别名（例如“常用卡”）。</param>
    /// <param name="cardNumber">卡号，必须非空。</param>
    /// <param name="securityNumber">安全码/CVV，必须非空。</param>
    /// <param name="cardHolderName">持卡人姓名，必须非空。</param>
    /// <param name="expiration">卡片过期时间，不能为过去的时间。</param>
    /// <exception cref="OrderingDomainException">当参数校验不通过时抛出领域异常。</exception>
    public PaymentMethod(int cardTypeId, string alias, string cardNumber, string securityNumber, string cardHolderName, DateTime expiration)
    {
        // 验证约束：卡号不能为空
        _cardNumber = !string.IsNullOrWhiteSpace(cardNumber) ? cardNumber : throw new OrderingDomainException(nameof(cardNumber));
        // 验证约束：安全码不能为空
        _securityNumber = !string.IsNullOrWhiteSpace(securityNumber) ? securityNumber : throw new OrderingDomainException(nameof(securityNumber));
        // 验证约束：持卡人姓名不能为空
        _cardHolderName = !string.IsNullOrWhiteSpace(cardHolderName) ? cardHolderName : throw new OrderingDomainException(nameof(cardHolderName));

        // 验证约束：过期时间不能是过去的时间
        if (expiration < DateTime.UtcNow)
        {
            throw new OrderingDomainException(nameof(expiration));
        }

        _alias = alias;
        _expiration = expiration;
        _cardTypeId = cardTypeId;
    }

    /// <summary>
    /// 判断当前支付方式是否与指定的卡类型、卡号及过期时间相同。
    /// </summary>
    /// <param name="cardTypeId">卡类型 ID。</param>
    /// <param name="cardNumber">卡号。</param>
    /// <param name="expiration">过期时间。</param>
    /// <returns>如果完全相同返回 true，否则返回 false。</returns>
    public bool IsEqualTo(int cardTypeId, string cardNumber, DateTime expiration)
    {
        return _cardTypeId == cardTypeId
            && _cardNumber == cardNumber
            && _expiration == expiration;
    }
}
