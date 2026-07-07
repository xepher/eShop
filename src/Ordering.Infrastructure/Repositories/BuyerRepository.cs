namespace eShop.Ordering.Infrastructure.Repositories;

/// <summary>
/// 买家聚合根 (Buyer Aggregate Root) 的仓储实现类，封装与数据库直接交互的细节。
/// </summary>
public class BuyerRepository
    : IBuyerRepository
{
    private readonly OrderingContext _context;

    /// <summary>
    /// 获取当前仓储关联的工作单元 (Unit of Work) 实例。
    /// </summary>
    public IUnitOfWork UnitOfWork => _context;

    /// <summary>
    /// 初始化 BuyerRepository 的新实例。
    /// </summary>
    /// <param name="context">订单数据库上下文。</param>
    public BuyerRepository(OrderingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 向买家集合中添加一个新的买家实体。
    /// </summary>
    /// <param name="buyer">待添加的买家实体对象。</param>
    /// <returns>返回受追踪的买家实体对象。</returns>
    public Buyer Add(Buyer buyer)
    {
        // 如果买家是瞬时的（即尚未在数据库生成标识），则将其添加至上下文中
        if (buyer.IsTransient())
        {
            return _context.Buyers
                .Add(buyer)
                .Entity;
        }

        return buyer;
    }

    /// <summary>
    /// 更新现有的买家实体信息。
    /// </summary>
    /// <param name="buyer">要更新的买家实体。</param>
    /// <returns>返回受追踪且已更新的实体。</returns>
    public Buyer Update(Buyer buyer)
    {
        return _context.Buyers
                .Update(buyer)
                .Entity;
    }

    /// <summary>
    /// 根据买家的唯一标识 (IdentityGuid) 异步查找买家实体，同时预加载其关联的支付方式列表。
    /// </summary>
    /// <param name="identity">买家的唯一身份 ID。</param>
    /// <returns>返回查找到的买家实体；若未找到，则返回 null。</returns>
    public async Task<Buyer> FindAsync(string identity)
    {
        var buyer = await _context.Buyers
            .Include(b => b.PaymentMethods)
            .Where(b => b.IdentityGuid == identity)
            .SingleOrDefaultAsync();

        return buyer;
    }

    /// <summary>
    /// 根据买家的自增主键 (Id) 异步查找买家实体，同时预加载其关联的支付方式列表。
    /// </summary>
    /// <param name="id">买家主键 ID。</param>
    /// <returns>返回查找到的买家实体；若未找到，则返回 null。</returns>
    public async Task<Buyer> FindByIdAsync(int id)
    {
        var buyer = await _context.Buyers
            .Include(b => b.PaymentMethods)
            .Where(b => b.Id == id)
            .SingleOrDefaultAsync();

        return buyer;
    }
}
