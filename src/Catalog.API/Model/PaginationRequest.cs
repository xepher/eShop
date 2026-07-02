using System.ComponentModel;

namespace eShop.Catalog.API.Model;

/// <summary>
/// 商品分页查询请求参数记录类。
/// </summary>
/// <param name="PageSize">单页大小（默认为 10）</param>
/// <param name="PageIndex">页码索引（0 代表第一页）</param>
public record PaginationRequest(
    [property: Description("Number of items to return in a single page of results")]
    [property: DefaultValue(10)]
    int PageSize = 10,

    [property: Description("The index of the page of results to return")]
    [property: DefaultValue(0)]
    int PageIndex = 0
);
