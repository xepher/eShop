using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.API.Model;

/// <summary>
/// 商品品牌实体模型
/// </summary>
public class CatalogBrand
{
    public CatalogBrand(string brand) {
        Brand = brand;
    }

    /// <summary>
    /// 自增唯一主键 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 品牌名称（如 .NET, Azure, SQL 等）
    /// </summary>
    [Required]
    public string Brand { get; set; }
}
