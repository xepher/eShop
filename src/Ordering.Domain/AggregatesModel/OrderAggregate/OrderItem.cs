using System.ComponentModel.DataAnnotations;

namespace eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

/// <summary>
/// 订单项实体。作为订单聚合根下的子实体，表示订单中包含的具体商品、数量、单价和折扣信息。
/// </summary>
public class OrderItem
    : Entity
{
    /// <summary>
    /// 商品名称。
    /// </summary>
    [Required]
    public string ProductName { get; private set; }
    
    /// <summary>
    /// 商品图片地址。
    /// </summary>
    public string PictureUrl { get; private set;}
    
    /// <summary>
    /// 商品单价。
    /// </summary>
    public decimal UnitPrice { get; private set;}
    
    /// <summary>
    /// 订单项折扣金额。
    /// </summary>
    public decimal Discount { get; private set; }
    
    /// <summary>
    /// 购买的商品数量。
    /// </summary>
    public int Units { get; private set; }

    /// <summary>
    /// 关联的商品 ID。
    /// </summary>
    public int ProductId { get; private set; }

    /// <summary>
    /// 用于 ORM (EF Core) 反序列化或映射的受保护无参构造函数。
    /// </summary>
    protected OrderItem() { }

    /// <summary>
    /// 创建订单项实体的构造函数。
    /// </summary>
    /// <param name="productId">商品唯一标识。</param>
    /// <param name="productName">商品名称。</param>
    /// <param name="unitPrice">单价。</param>
    /// <param name="discount">折扣。</param>
    /// <param name="pictureUrl">商品图片路径。</param>
    /// <param name="units">购买数量，默认为 1。</param>
    /// <exception cref="OrderingDomainException">当购买数量小于等于 0，或折扣金额大于订单项总金额时抛出。</exception>
    public OrderItem(int productId, string productName, decimal unitPrice, decimal discount, string pictureUrl, int units = 1)
    {
        // 验证约束：购买商品数量必须大于 0
        if (units <= 0)
        {
            throw new OrderingDomainException("Invalid number of units");
        }

        // 验证约束：订单项的总金额（单价 * 数量）不能低于应用的折扣金额
        if ((unitPrice * units) < discount)
        {
            throw new OrderingDomainException("The total of order item is lower than applied discount");
        }

        ProductId = productId;

        ProductName = productName;
        UnitPrice = unitPrice;
        Discount = discount;
        Units = units;
        PictureUrl = pictureUrl;
    }
    
    /// <summary>
    /// 设置订单项的新折扣额。
    /// </summary>
    /// <param name="discount">新的折扣额。</param>
    /// <exception cref="OrderingDomainException">当折扣额小于 0 时抛出。</exception>
    public void SetNewDiscount(decimal discount)
    {
        // 验证约束：折扣额不能为负数
        if (discount < 0)
        {
            throw new OrderingDomainException("Discount is not valid");
        }

        Discount = discount;
    }

    /// <summary>
    /// 增加当前订单项的商品购买数量。
    /// </summary>
    /// <param name="units">要增加的商品数量。</param>
    /// <exception cref="OrderingDomainException">当增加的数量小于 0 时抛出。</exception>
    public void AddUnits(int units)
    {
        // 验证约束：增加的数量不能为负数
        if (units < 0)
        {
            throw new OrderingDomainException("Invalid units");
        }

        Units += units;
    }
}
