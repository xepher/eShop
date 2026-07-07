namespace eShop.IntegrationEventLogEF.Services;

/// <summary>
/// 集成事件日志服务接口。
/// 定义了对本地 Outbox 事务事件日志进行保存、提取及状态更新的契约。
/// </summary>
public interface IIntegrationEventLogService
{
    /// <summary>
    /// 异步获取与指定事务关联、且尚未发布（NotPublished）的所有集成事件日志条目。
    /// </summary>
    /// <param name="transactionId">目标数据库事务唯一标识。</param>
    /// <returns>返回反序列化后的事件日志条目集合。</returns>
    Task<IEnumerable<IntegrationEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId);

    /// <summary>
    /// 将一个新的集成事件与当前本地数据库事务绑定并保存到本地 Outbox 日志表中。
    /// </summary>
    /// <param name="event">待保存的集成事件对象。</param>
    /// <param name="transaction">当前的 EF Core 数据库事务上下文。</param>
    Task SaveEventAsync(IntegrationEvent @event, IDbContextTransaction transaction);

    /// <summary>
    /// 标记指定的集成事件状态为“已发布（Published）”。
    /// </summary>
    /// <param name="eventId">已发布成功的事件唯一标识。</param>
    Task MarkEventAsPublishedAsync(Guid eventId);

    /// <summary>
    /// 标记指定的集成事件状态为“正在发布中（InProgress）”，并递增发送次数。
    /// </summary>
    /// <param name="eventId">待发布事件的唯一标识。</param>
    Task MarkEventAsInProgressAsync(Guid eventId);

    /// <summary>
    /// 标记指定的集成事件状态为“发布失败（PublishedFailed）”。
    /// </summary>
    /// <param name="eventId">发布失败事件的唯一标识。</param>
    Task MarkEventAsFailedAsync(Guid eventId);
}

