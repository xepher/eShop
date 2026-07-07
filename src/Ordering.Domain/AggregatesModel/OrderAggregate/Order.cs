using System.ComponentModel.DataAnnotations;

namespace eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

/// <summary>
/// 订单聚合根。管理订单的生命周期、订单项、状态转换和相关的领域事件。
/// 符合 DDD 规范，确保所有业务规则和不变性约束在聚合根内得到满足和强制执行。
/// </summary>
public class Order
    : Entity, IAggregateRoot
{
    /// <summary>
    /// 订单创建时间。
    /// </summary>
    public DateTime OrderDate { get; private set; }

    /// <summary>
    /// 配送地址。
    /// 地址是一个值对象（Value Object），在 EF Core 中被持久化为拥有的实体（Owned Entity）。
    /// </summary>
    [Required]
    public Address Address { get; private set; }

    /// <summary>
    /// 买家 ID（外部键/标识）。
    /// </summary>
    public int? BuyerId { get; private set; }

    /// <summary>
    /// 买家实体导航属性。
    /// </summary>
    public Buyer Buyer { get; }

    /// <summary>
    /// 订单当前状态。
    /// </summary>
    public OrderStatus OrderStatus { get; private set; }
    
    /// <summary>
    /// 订单状态或生命周期变化的描述说明。
    /// </summary>
    public string Description { get; private set; }

    // 草稿订单标识。目前在此处设置，但未在其他地方显式检查，可以根据未来需要启用。
#pragma warning disable CS0414 // The field 'Order._isDraft' is assigned but its value is never used
    private bool _isDraft;
#pragma warning restore CS0414 

    /// <summary>
    /// 订单项内部列表。
    /// 使用私有集合字段是 DDD 聚合设计的最佳实践，可防止从聚合根外部直接修改订单子项，
    /// 所有新增或修改订单项的操作必须通过聚合根的方法（如 AddOrderItem）进行，以确保业务逻辑的一致性。
    /// </summary>
    private readonly List<OrderItem> _orderItems;
   
    /// <summary>
    /// 订单项只读视图，对外暴露以维护聚合根的封装性。
    /// </summary>
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    /// <summary>
    /// 支付方式 ID（外部键/标识）。
    /// </summary>
    public int? PaymentId { get; private set; }

    /// <summary>
    /// 静态工厂方法，创建一个草稿状态的订单。
    /// </summary>
    /// <returns>草稿订单实例。</returns>
    public static Order NewDraft()
    {
        var order = new Order
        {
            _isDraft = true
        };
        return order;
    }

    /// <summary>
    /// 用于 ORM (EF Core) 反序列化或映射的受保护无参构造函数。
    /// </summary>
    protected Order()
    {
        _orderItems = new List<OrderItem>();
        _isDraft = false;
    }

    /// <summary>
    /// 创建新订单的构造函数。
    /// 默认状态为 Submitted，并触发 OrderStartedDomainEvent 领域事件。
    /// </summary>
    /// <param name="userId">用户唯一标识（IDP 用户 ID）。</param>
    /// <param name="userName">用户名。</param>
    /// <param name="address">配送地址值对象。</param>
    /// <param name="cardTypeId">信用卡类型 ID。</param>
    /// <param name="cardNumber">卡号。</param>
    /// <param name="cardSecurityNumber">安全码。</param>
    /// <param name="cardHolderName">持卡人姓名。</param>
    /// <param name="cardExpiration">卡过期时间。</param>
    /// <param name="buyerId">买家 ID（如果已存在该买家）。</param>
    /// <param name="paymentMethodId">支付方式 ID（如果已存在该支付方式）。</param>
    public Order(string userId, string userName, Address address, int cardTypeId, string cardNumber, string cardSecurityNumber,
            string cardHolderName, DateTime cardExpiration, int? buyerId = null, int? paymentMethodId = null) : this()
    {
        BuyerId = buyerId;
        PaymentId = paymentMethodId;
        OrderStatus = OrderStatus.Submitted;
        OrderDate = DateTime.UtcNow;
        Address = address;

        // 触发 OrderStartedDomainEvent 领域事件。
        // 该事件在 DbContext 提交更改后被分发，用于创建或验证买家及支付方式。
        AddOrderStartedDomainEvent(userId, userName, cardTypeId, cardNumber,
                                    cardSecurityNumber, cardHolderName, cardExpiration);
    }

    /// <summary>
    /// 向订单中添加或更新订单项（商品）。
    /// 这是向订单添加商品的唯一合法途径，确保业务规则在聚合根内部强制执行。
    /// </summary>
    /// <param name="productId">商品 ID。</param>
    /// <param name="productName">商品名称。</param>
    /// <param name="unitPrice">单价。</param>
    /// <param name="discount">折扣额。</param>
    /// <param name="pictureUrl">商品图片 URL。</param>
    /// <param name="units">购买数量，默认为 1。</param>
    public void AddOrderItem(int productId, string productName, decimal unitPrice, decimal discount, string pictureUrl, int units = 1)
    {
        var existingOrderForProduct = _orderItems.SingleOrDefault(o => o.ProductId == productId);

        if (existingOrderForProduct != null)
        {
            // 约束：如果已存在相同商品的订单项，则合并数量，且保留较大的折扣额
            if (discount > existingOrderForProduct.Discount)
            {
                existingOrderForProduct.SetNewDiscount(discount);
            }

            existingOrderForProduct.AddUnits(units);
        }
        else
        {
            // 如果不存在，则创建并添加经过验证的全新订单项实体
            var orderItem = new OrderItem(productId, productName, unitPrice, discount, pictureUrl, units);
            _orderItems.Add(orderItem);
        }
    }

    /// <summary>
    /// 设置经过验证的买家和支付方式标识（供买家与支付验证领域事件的订阅处理器调用）。
    /// </summary>
    /// <param name="buyerId">买家 ID。</param>
    /// <param name="paymentId">支付方式 ID。</param>
    public void SetPaymentMethodVerified(int buyerId, int paymentId)
    {
        BuyerId = buyerId;
        PaymentId = paymentId;
    }
    
    /// <summary>
    /// 将订单状态变更为“等待验证”。
    /// 仅限处于“已提交”状态的订单。
    /// </summary>
    public void SetAwaitingValidationStatus()
    {
        if (OrderStatus == OrderStatus.Submitted)
        {
            // 触发状态变更至等待验证的领域事件，以便触发后续业务流（如库存检查）
            AddDomainEvent(new OrderStatusChangedToAwaitingValidationDomainEvent(Id, _orderItems));
            OrderStatus = OrderStatus.AwaitingValidation;
        }
    }

    /// <summary>
    /// 将订单状态变更为“库存已确认”。
    /// 仅限处于“等待验证”状态的订单。
    /// </summary>
    public void SetStockConfirmedStatus()
    {
        if (OrderStatus == OrderStatus.AwaitingValidation)
        {
            // 触发状态变更至库存已确认领域事件，以便触发支付请求
            AddDomainEvent(new OrderStatusChangedToStockConfirmedDomainEvent(Id));

            OrderStatus = OrderStatus.StockConfirmed;
            Description = "All the items were confirmed with available stock.";
        }
    }

    /// <summary>
    /// 将订单状态变更为“已付款”。
    /// 仅限处于“库存已确认”状态的订单。
    /// </summary>
    public void SetPaidStatus()
    {
        if (OrderStatus == OrderStatus.StockConfirmed)
        {
            // 触发付款成功的领域事件（可用于开具发票、发货通知等）
            AddDomainEvent(new OrderStatusChangedToPaidDomainEvent(Id, OrderItems));

            OrderStatus = OrderStatus.Paid;
            Description = "The payment was performed at a simulated \"American Bank checking bank account ending on XX35071\"";
        }
    }

    /// <summary>
    /// 将订单状态变更为“已发货”。
    /// 约束：订单必须已付款。
    /// </summary>
    /// <exception cref="OrderingDomainException">当订单不是“已付款”状态时，不允许发货，抛出领域异常。</exception>
    public void SetShippedStatus()
    {
        if (OrderStatus != OrderStatus.Paid)
        {
            StatusChangeException(OrderStatus.Shipped);
        }

        OrderStatus = OrderStatus.Shipped;
        Description = "The order was shipped.";
        // 触发发货领域事件
        AddDomainEvent(new OrderShippedDomainEvent(this));
    }

    /// <summary>
    /// 将订单状态变更为“已取消”。
    /// 约束：如果订单已经付款或已发货，则不能取消。
    /// </summary>
    /// <exception cref="OrderingDomainException">当订单已经付款或已发货时，抛出领域异常。</exception>
    public void SetCancelledStatus()
    {
        if (OrderStatus == OrderStatus.Paid ||
            OrderStatus == OrderStatus.Shipped)
        {
            StatusChangeException(OrderStatus.Cancelled);
        }

        OrderStatus = OrderStatus.Cancelled;
        Description = "The order was cancelled.";
        // 触发取消订单领域事件
        AddDomainEvent(new OrderCancelledDomainEvent(this));
    }

    /// <summary>
    /// 当库存不足或被拒时，将处于“等待验证”状态的订单变更为“已取消”，并说明被拒商品详情。
    /// </summary>
    /// <param name="orderStockRejectedItems">被拒的商品 ID 集合。</param>
    public void SetCancelledStatusWhenStockIsRejected(IEnumerable<int> orderStockRejectedItems)
    {
        if (OrderStatus == OrderStatus.AwaitingValidation)
        {
            OrderStatus = OrderStatus.Cancelled;

            // 获取被拒商品的产品名称，拼接进订单描述中
            var itemsStockRejectedProductNames = OrderItems
                .Where(c => orderStockRejectedItems.Contains(c.ProductId))
                .Select(c => c.ProductName);

            var itemsStockRejectedDescription = string.Join(", ", itemsStockRejectedProductNames);
            Description = $"The product items don't have stock: ({itemsStockRejectedDescription}).";
        }
    }

    /// <summary>
    /// 触发订单启动领域事件，开始买家和付款方式验证流程。
    /// </summary>
    private void AddOrderStartedDomainEvent(string userId, string userName, int cardTypeId, string cardNumber,
            string cardSecurityNumber, string cardHolderName, DateTime cardExpiration)
    {
        var orderStartedDomainEvent = new OrderStartedDomainEvent(this, userId, userName, cardTypeId,
                                                                    cardNumber, cardSecurityNumber,
                                                                    cardHolderName, cardExpiration);

        this.AddDomainEvent(orderStartedDomainEvent);
    }

    /// <summary>
    /// 抛出不支持的状态变更领域异常。
    /// </summary>
    /// <param name="orderStatusToChange">目标状态。</param>
    /// <exception cref="OrderingDomainException">状态无效变更异常。</exception>
    private void StatusChangeException(OrderStatus orderStatusToChange)
    {
        throw new OrderingDomainException($"Is not possible to change the order status from {OrderStatus} to {orderStatusToChange}.");
    }

    /// <summary>
    /// 计算订单中所有商品项的订单总额。
    /// </summary>
    /// <returns>订单总金额。</returns>
    public decimal GetTotal() => _orderItems.Sum(o => o.Units * o.UnitPrice);
}
