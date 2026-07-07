namespace eShop.IntegrationEventLogEF.Services;

/// <summary>
/// 集成事件日志服务实现类。
/// 泛型 <typeparamref name="TContext"/> 必须为 EF Core 的 <see cref="DbContext"/>，通常是各个业务 API 微服务自己的数据库上下文。
/// 实现了 Outbox（发件箱）模式，在同一个数据库连接和事务中将业务数据和事件日志一并写入。
/// </summary>
public class IntegrationEventLogService<TContext> : IIntegrationEventLogService, IDisposable
    where TContext : DbContext
{
    // 用于线程安全的释放状态标记
    private volatile bool _disposedValue;
    // 注入的数据库上下文
    private readonly TContext _context;
    // 缓存当前入口程序集中所有继承自 IntegrationEvent 的事件类型，避免在反序列化时频繁通过反射扫描程序集
    private readonly Type[] _eventTypes;

    /// <summary>
    /// 初始化集成事件日志服务。
    /// </summary>
    /// <param name="context">具体的数据库上下文实例。</param>
    public IntegrationEventLogService(TContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        
        // 动态加载入口程序集，获取所有名称以 "IntegrationEvent" 结尾的强类型事件
        _eventTypes = Assembly.Load(Assembly.GetEntryAssembly().FullName)
            .GetTypes()
            .Where(t => t.Name.EndsWith(nameof(IntegrationEvent)))
            .ToArray();
    }

    /// <summary>
    /// 获取属于指定事务、且状态为未发布的事件日志条目，并反序列化回强类型的集成事件对象。
    /// </summary>
    public async Task<IEnumerable<IntegrationEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId)
    {
        // 1. 查询数据库中符合当前事务 ID 且尚未发布的日志条目
        var result = await _context.Set<IntegrationEventLogEntry>()
            .Where(e => e.TransactionId == transactionId && e.State == EventStateEnum.NotPublished)
            .ToListAsync();

        if (result.Count != 0)
        {
            // 2. 按创建时间排序，并循环对每一条日志进行反序列化
            return result.OrderBy(o => o.CreationTime)
                .Select(e => e.DeserializeJsonContent(_eventTypes.FirstOrDefault(t => t.Name == e.EventTypeShortName)));
        }

        return [];
    }

    /// <summary>
    /// 在当前事务下保存集成事件日志条目。
    /// </summary>
    public Task SaveEventAsync(IntegrationEvent @event, IDbContextTransaction transaction)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));

        // 1. 构建事件日志条目对象
        var eventLogEntry = new IntegrationEventLogEntry(@event, transaction.TransactionId);

        // 2. 核心：强制使事件日志上下文共享主业务上下文的数据库事务连接，确保二者位于同一个原子事务中
        _context.Database.UseTransaction(transaction.GetDbTransaction());
        _context.Set<IntegrationEventLogEntry>().Add(eventLogEntry);

        // 3. 异步提交日志存储变更
        return _context.SaveChangesAsync();
    }

    /// <summary>
    /// 将事件状态修改为已发布。
    /// </summary>
    public Task MarkEventAsPublishedAsync(Guid eventId)
    {
        return UpdateEventStatus(eventId, EventStateEnum.Published);
    }

    /// <summary>
    /// 将事件状态修改为进行中。
    /// </summary>
    public Task MarkEventAsInProgressAsync(Guid eventId)
    {
        return UpdateEventStatus(eventId, EventStateEnum.InProgress);
    }

    /// <summary>
    /// 将事件状态修改为发送失败。
    /// </summary>
    public Task MarkEventAsFailedAsync(Guid eventId)
    {
        return UpdateEventStatus(eventId, EventStateEnum.PublishedFailed);
    }

    /// <summary>
    /// 统一更新集成事件状态的私有方法。
    /// </summary>
    private Task UpdateEventStatus(Guid eventId, EventStateEnum status)
    {
        // 查找并加载要修改的日志条目
        var eventLogEntry = _context.Set<IntegrationEventLogEntry>().Single(ie => ie.EventId == eventId);
        eventLogEntry.State = status;

        // 如果状态为发布进行中，累加尝试发送次数
        if (status == EventStateEnum.InProgress)
            eventLogEntry.TimesSent++;

        return _context.SaveChangesAsync();
    }

    /// <summary>
    /// 释放数据库上下文及相关托管资源。
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _context.Dispose();
            }

            _disposedValue = true;
        }
    }

    /// <summary>
    /// 显式调用垃圾回收资源清理。
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

