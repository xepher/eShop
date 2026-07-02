using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.API.Model;

/// <summary>
/// 商品分类实体模型
/// </summary>
public class CatalogType
{
    public CatalogType(string type) {
        Type = type;
    }

    /// <summary>
    /// 自增唯一主键 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 类别名称（如 Mug, T-Shirt, Sheet 等）
    /// </summary>
    [Required]
    public string Type { get; set; }
}
