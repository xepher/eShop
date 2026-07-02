using System.Text.Json.Serialization;

namespace eShop.Catalog.API.Model;

/// <summary>
/// 通用分页数据包装类。
/// </summary>
/// <typeparam name="TEntity">泛型实体类型</typeparam>
public class PaginatedItems<TEntity>(int pageIndex, int pageSize, long count, IEnumerable<TEntity> data) where TEntity : class
{
    /// <summary>
    /// 当前页码索引
    /// </summary>
    public int PageIndex { get; } = pageIndex;

    /// <summary>
    /// 每页数据条数
    /// </summary>
    public int PageSize { get; } = pageSize;

    /// <summary>
    /// 符合筛选条件的数据总条数
    /// </summary>
    public long Count { get; } = count;

    /// <summary>
    /// 当前页的实体数据集合
    /// </summary>
    public IEnumerable<TEntity> Data { get;} = data;
}
