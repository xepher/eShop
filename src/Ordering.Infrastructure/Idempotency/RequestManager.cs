namespace eShop.Ordering.Infrastructure.Idempotency;

/// <summary>
/// 幂等请求管理器实现类，利用 EF Core 及数据库的主键唯一约束来跟踪和阻止重复请求。
/// </summary>
public class RequestManager : IRequestManager
{
    private readonly OrderingContext _context;

    /// <summary>
    /// 初始化 RequestManager 的新实例。
    /// </summary>
    /// <param name="context">订单数据库上下文。</param>
    public RequestManager(OrderingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 异步判断指定请求主键 (Guid) 是否已经在数据库中存在。
    /// </summary>
    /// <param name="id">请求唯一标识。</param>
    /// <returns>若已记录该请求，则返回 true，否则返回 false。</returns>
    public async Task<bool> ExistAsync(Guid id)
    {
        // 查找 ClientRequest 记录
        var request = await _context.
            FindAsync<ClientRequest>(id);

        return request != null;
    }

    /// <summary>
    /// 异步为特定命令类型记录一个新的请求，若请求已存在则抛出领域异常，以此实现幂等性拦截。
    /// </summary>
    /// <typeparam name="T">命令请求的类型。</typeparam>
    /// <param name="id">请求唯一标识 (Guid)。</param>
    /// <exception cref="OrderingDomainException">当请求已存在时抛出，指示发生了重复处理请求的操作。</exception>
    public async Task CreateRequestForCommandAsync<T>(Guid id)
    {
        var exists = await ExistAsync(id);

        // 如果请求已存在，抛出异常阻断业务流程；否则创建新记录
        var request = exists ?
            throw new OrderingDomainException($"Request with {id} already exists") :
            new ClientRequest()
            {
                Id = id,
                Name = typeof(T).Name,
                Time = DateTime.UtcNow
            };

        // 添加并立刻保存到数据库中以确立主键独占性
        _context.Add(request);

        await _context.SaveChangesAsync();
    }
}
