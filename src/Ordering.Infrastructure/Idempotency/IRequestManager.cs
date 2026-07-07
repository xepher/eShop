namespace eShop.Ordering.Infrastructure.Idempotency;

/// <summary>
/// 幂等请求管理器接口，用于确保特定命令请求在微服务中仅被处理一次。
/// </summary>
public interface IRequestManager
{
    /// <summary>
    /// 异步判断指定 ID 的请求是否已经存在（即是否已被处理过）。
    /// </summary>
    /// <param name="id">请求唯一标识 (Guid)。</param>
    /// <returns>若存在返回 true，否则返回 false。</returns>
    Task<bool> ExistAsync(Guid id);

    /// <summary>
    /// 异步为特定命令创建并记录一个新的请求记录，标识该请求已开始/完成处理。
    /// </summary>
    /// <typeparam name="T">命令的类型类型。</typeparam>
    /// <param name="id">请求唯一标识 (Guid)。</param>
    /// <returns>异步任务。</returns>
    Task CreateRequestForCommandAsync<T>(Guid id);
}
