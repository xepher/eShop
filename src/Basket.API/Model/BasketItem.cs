namespace eShop.Basket.API.Model;

/// <summary>
/// 购物车商品明细模型类，包含商品基本属性，并实现 IValidatableObject 接口支持业务逻辑自我验证。
/// </summary>
public class BasketItem : IValidatableObject
{
    [Required]
    public string Id { get; set; } = default!;

    [Required]
    public int ProductId { get; set; }

    [Required]
    public string ProductName { get; set; } = default!;

    public decimal UnitPrice { get; set; }
    public decimal OldUnitPrice { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public string PictureUrl { get; set; } = default!;

    /// <summary>
    /// 模型级字段自我校验逻辑。
    /// 确保购物车内商品选购数量必须至少为 1 件。
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (Quantity < 1)
        {
            results.Add(new ValidationResult("Invalid number of units", new[] { "Quantity" }));
        }

        return results;
    }
}
