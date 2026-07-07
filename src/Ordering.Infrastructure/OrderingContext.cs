using eShop.IntegrationEventLogEF;

namespace eShop.Ordering.Infrastructure;

/// <summary>
/// 订单系统的 Entity Framework Core 数据库上下文，同时实现了领域驱动设计 (DDD) 中的工作单元 (Unit of Work) 接口。
/// </summary>
/// <remarks>
/// 在 'Ordering.Infrastructure' 项目目录下使用以下命令添加数据库迁移：
///
/// dotnet ef migrations add --startup-project Ordering.API --context OrderingContext [migration-name]
/// </remarks>
public class OrderingContext : DbContext, IUnitOfWork
{
    /// <summary>
    /// 订单聚合根 (Order Aggregate Root) 的 DbSet 集合。
    /// </summary>
    public DbSet<Order> Orders { get; set; }

    /// <summary>
    /// 订单项实体 (OrderItem Entity，属于 Order 聚合) 的 DbSet 集合。
    /// </summary>
    public DbSet<OrderItem> OrderItems { get; set; }

    /// <summary>
    /// 支付方式实体 (PaymentMethod Entity，属于 Buyer 聚合) 的 DbSet 集合。
    /// </summary>
    public DbSet<PaymentMethod> Payments { get; set; }

    /// <summary>
    /// 买家聚合根 (Buyer Aggregate Root) 的 DbSet 集合。
    /// </summary>
    public DbSet<Buyer> Buyers { get; set; }

    /// <summary>
    /// 卡片类型 (CardType 值对象/只读实体，作为基础数据模型) 的 DbSet 集合。
    /// </summary>
    public DbSet<CardType> CardTypes { get; set; }

    private readonly IMediator _mediator;
    private IDbContextTransaction _currentTransaction;

    /// <summary>
    /// 仅带有配置参数的构造函数（通常用于设计时或未注入 Mediator 的场景）。
    /// </summary>
    /// <param name="options">数据库上下文配置选项。</param>
    public OrderingContext(DbContextOptions<OrderingContext> options) : base(options) { }

    /// <summary>
    /// 获取当前活动的数据库事务。
    /// </summary>
    /// <returns>当前事务对象，若没有活动事务则返回 null。</returns>
    public IDbContextTransaction GetCurrentTransaction() => _currentTransaction;

    /// <summary>
    /// 获取一个值，指示当前是否存在活动的数据库事务。
    /// </summary>
    public bool HasActiveTransaction => _currentTransaction != null;

    /// <summary>
    /// 初始化 OrderingContext 的新实例，并注入 MediatR 介质器用于分发领域事件。
    /// </summary>
    /// <param name="options">数据库上下文配置选项。</param>
    /// <param name="mediator">MediatR 接口，用于在持久化时分发领域事件。</param>
    public OrderingContext(DbContextOptions<OrderingContext> options, IMediator mediator) : base(options)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        System.Diagnostics.Debug.WriteLine("OrderingContext::ctor ->" + this.GetHashCode());
    }

    /// <summary>
    /// 配置实体映射关系与约束规则。
    /// </summary>
    /// <param name="modelBuilder">模型构建器。</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 设置默认的数据库 Schema 名称
        modelBuilder.HasDefaultSchema("ordering");

        // 应用各个实体的具体配置映射类
        modelBuilder.ApplyConfiguration(new ClientRequestEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentMethodEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OrderEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CardTypeEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new BuyerEntityTypeConfiguration());

        // 使用集成事件日志配置 (Outbox 模式，确保业务数据和集成事件一致性)
        modelBuilder.UseIntegrationEventLogs();
    }

    /// <summary>
    /// 保存所有更改到数据库，在此过程中会触发并分发领域事件 (Domain Events)。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>操作是否成功。</returns>
    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // 分发领域事件集合。
        // 选择策略：
        // A) 在将数据提交（EF SaveChanges）到数据库【之前】分发领域事件。
        //    这会使得领域事件处理程序（Domain Event Handlers）执行的所有副作用操作，
        //    与当前 Command 处理程序使用同一个 DbContext 并在同一个数据库事务中执行。
        //    （适用于强一致性或同一作用域内的事务模型）
        // B) 在将数据提交（EF SaveChanges）到数据库【之后】分发领域事件。
        //    这会产生多个事务。此时需要手动处理最终一致性（Eventual Consistency）以及当某个 Handler 执行失败时的补偿操作。
        await _mediator.DispatchDomainEventsAsync(this);

        // 执行此行后，通过该 DbContext 进行的所有更改（包括 Command 处理程序和领域事件处理程序中的更改）都将提交到数据库
        _ = await base.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// 异步开启一个新的数据库事务，隔离级别为已提交读 (ReadCommitted)。
    /// </summary>
    /// <returns>数据库事务实例。</returns>
    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction != null) return null;

        _currentTransaction = await Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        return _currentTransaction;
    }

    /// <summary>
    /// 异步提交事务，确保在提交前保存所有挂起的更改。
    /// </summary>
    /// <param name="transaction">要提交的事务。</param>
    /// <exception cref="ArgumentNullException">传入事务为 null 时抛出。</exception>
    /// <exception cref="InvalidOperationException">传入事务不是当前活动事务时抛出。</exception>
    public async Task CommitTransactionAsync(IDbContextTransaction transaction)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction != _currentTransaction) throw new InvalidOperationException($"Transaction {transaction.TransactionId} is not current");

        try
        {
            // 保存未保存的更改
            await SaveChangesAsync();
            // 提交底层数据库事务
            await transaction.CommitAsync();
        }
        catch
        {
            // 发生异常时回滚当前事务
            RollbackTransaction();
            throw;
        }
        finally
        {
            // 清理并释放事务资源
            if (HasActiveTransaction)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    /// <summary>
    /// 回滚当前的数据库事务，并释放相关资源。
    /// </summary>
    public void RollbackTransaction()
    {
        try
        {
            _currentTransaction?.Rollback();
        }
        finally
        {
            if (HasActiveTransaction)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }
}

#nullable enable
