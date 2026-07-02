namespace eShop.Catalog.API;

/// <summary>
/// 商品目录服务的强类型配置选项类。
/// </summary>
public class CatalogOptions
{
    /// <summary>
    /// 商品图片文件的基础托管 URL（PicBaseUrl），用于拼接完整的图片链接
    /// </summary>
    public string? PicBaseUrl { get; set; }

    /// <summary>
    /// 指示是否加载自定义数据或本地测试假数据
    /// </summary>
    public bool UseCustomizationData { get; set; }
}
