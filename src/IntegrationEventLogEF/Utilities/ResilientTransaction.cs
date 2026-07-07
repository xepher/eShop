namespace eShop.IntegrationEventLogEF.Utilities;

/// <summary>
/// 弹性事务工具类。
/// 封装了 EF Core 的连接恢复策略（ExecutionStrategy），用于确保在数据库连接异常、临时网络中断或锁冲突时，能够自动重试事务操作。
/// </summary>
public class ResilientTransaction
{
    private readonly DbContext _context;

    /// <summary>
    /// 构造函数，传入数据库上下文实例。
    /// </summary>
    private ResilientTransaction(DbContext context) =>
        _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <summary>
    /// 创建一个新的弹性事务构建器。
    /// </summary>
    /// <param name="context">数据库上下文实例。</param>
    /// <returns>返回 ResilientTransaction 实例。</returns>
    public static ResilientTransaction New(DbContext context) => new(context);

    /// <summary>
    /// 执行一个包含在重试策略和事务之中的异步操作。
    /// 如果操作在执行过程中由于暂时性数据库故障失败，EF Core 的 ExecutionStrategy 会自动进行重试。
    /// </summary>
    /// <param name="action">要在事务中执行的异步操作委托。</param>
    public async Task ExecuteAsync(Func<Task> action)
    {
        // 使用 EF Core 弹性重试策略处理多个操作或显式事务启动。
        // 参考：https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // 开启一个原子事务
            await using var transaction = await _context.Database.BeginTransactionAsync();
            
            // 执行具体的业务逻辑（通常是保存业务实体以及保存 Outbox 事件日志）
            await action();
            
            // 提交事务。如果中途报错，整个事务会自动回滚
            await transaction.CommitAsync();
        });
    }
}

