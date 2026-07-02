using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Pgvector;

namespace eShop.Catalog.API.Model;

/// <summary>
/// 商品目录条目（商品实体模型）
/// 包含基本信息、库存阈值、以及用于语义检索的 Embedding 向量。
/// </summary>
public class CatalogItem
{
    public int Id { get; set; }

    /// <summary>
    /// 商品名称
    /// </summary>
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// 商品描述信息
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 商品单价
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 商品图片的文件名（例如 1.webp）
    /// </summary>
    public string? PictureFileName { get; set; }

    /// <summary>
    /// 商品分类 ID
    /// </summary>
    public int CatalogTypeId { get; set; }

    /// <summary>
    /// 商品分类关联导航属性
    /// </summary>
    public CatalogType? CatalogType { get; set; }

    /// <summary>
    /// 商品品牌 ID
    /// </summary>
    public int CatalogBrandId { get; set; }

    /// <summary>
    /// 商品品牌关联导航属性
    /// </summary>
    public CatalogBrand? CatalogBrand { get; set; }

    /// <summary>
    /// 现存可用库存数量 (Quantity in stock)
    /// </summary>
    public int AvailableStock { get; set; }

    /// <summary>
    /// 补货阈值 (RestockThreshold)：当库存降至此值时，应当发起补货流程
    /// </summary>
    public int RestockThreshold { get; set; }

    /// <summary>
    /// 最大库存上限 (MaxStockThreshold)：受仓库物理/物流条件制约，最大可入库的数量
    /// </summary>
    public int MaxStockThreshold { get; set; }

    /// <summary>
    /// AI 语义检索所需的商品描述向量嵌入 (Embedding Vector)。
    /// 标记为 [JsonIgnore]，避免该长向量浮点数组泄露到对外公开的 RESTful API 中。
    /// </summary>
    [JsonIgnore]
    public Vector? Embedding { get; set; }

    /// <summary>
    /// 标识商品当前是否处于“补货处理中”的状态
    /// </summary>
    public bool OnReorder { get; set; }

    public CatalogItem(string name) { Name = name; }


    /// <summary>
    /// 扣减商品库存。
    /// 扣减成功后会返回实际移出的数量，以支持库存不足时的部分出库机制。
    /// </summary>
    /// <param name="quantityDesired">期望扣减的库存数量</param>
    /// <returns>实际完成扣减的库存数量</returns>
    public int RemoveStock(int quantityDesired)
    {
        if (AvailableStock == 0)
        {
            throw new CatalogDomainException($"Empty stock, product item {Name} is sold out");
        }

        if (quantityDesired <= 0)
        {
            throw new CatalogDomainException($"Item units desired should be greater than zero");
        }

        int removed = Math.Min(quantityDesired, this.AvailableStock);

        this.AvailableStock -= removed;

        return removed;
    }

    /// <summary>
    /// 增加商品库存（入库）。
    /// 如果入库后数量超过最大库存阈值，则自动进行截断，仅增加到最大阈值。
    /// </summary>
    /// <param name="quantity">入库数量</param>
    /// <returns>实际被成功添加的库存数量</returns>
    public int AddStock(int quantity)
    {
        int original = this.AvailableStock;

        // 如果入库后总库存会超过限制的最大库存阈值
        if ((this.AvailableStock + quantity) > this.MaxStockThreshold)
        {
            // 截断到最大限制
            this.AvailableStock += (this.MaxStockThreshold - this.AvailableStock);
        }
        else
        {
            this.AvailableStock += quantity;
        }

        // 成功补货，重置补货中状态为 false
        this.OnReorder = false;

        return this.AvailableStock - original;
    }
}
