namespace eShop.Ordering.Infrastructure.Repositories;

/// <summary>
/// 订单聚合根 (Order Aggregate Root) 的仓储实现类，提供对订单的增删改查支持。
/// </summary>
public class OrderRepository
    : IOrderRepository
{
    private readonly OrderingContext _context;

    /// <summary>
    /// 获取当前仓储关联的工作单元 (Unit of Work) 实例。
    /// </summary>
    public IUnitOfWork UnitOfWork => _context;

    /// <summary>
    /// 初始化 OrderRepository 的新实例。
    /// </summary>
    /// <param name="context">订单数据库上下文。</param>
    public OrderRepository(OrderingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 向数据库上下文添加一个新的订单。
    /// </summary>
    /// <param name="order">需要保存的订单实体。</param>
    /// <returns>返回受 EF Core 追踪的订单实体。</returns>
    public Order Add(Order order)
    {
        return _context.Orders.Add(order).Entity;
    }

    /// <summary>
    /// 异步获取指定的订单，并显式加载其关联的订单子项 (OrderItems)。
    /// </summary>
    /// <param name="orderId">订单的唯一主键标识。</param>
    /// <returns>返回包含订单项详情的订单聚合根对象；若不存在则返回 null。</returns>
    public async Task<Order> GetAsync(int orderId)
    {
        // 首先查找订单主表
        var order = await _context.Orders.FindAsync(orderId);

        // 如果订单存在，则显式异步加载 OrderItems 子集合，避免全表关联带来的性能问题或延迟加载需求
        if (order != null)
        {
            await _context.Entry(order)
                .Collection(i => i.OrderItems).LoadAsync();
        }

        return order;
    }

    /// <summary>
    /// 将订单的状态标记为已修改，从而让 EF Core 在下一次 SaveChanges 时执行更新操作。
    /// </summary>
    /// <param name="order">已修改的订单聚合根对象。</param>
    public void Update(Order order)
    {
        _context.Entry(order).State = EntityState.Modified;
    }
}
