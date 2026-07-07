namespace eShop.Ordering.Infrastructure.EntityConfigurations;

/// <summary>
/// 订单聚合根 (Order Aggregate Root) 的 EF Core 实体映射配置类。
/// </summary>
class OrderEntityTypeConfiguration : IEntityTypeConfiguration<Order>
{
    /// <summary>
    /// 配置订单实体的表映射、主键生成方案、值对象拥有类型以及外键约束关系。
    /// </summary>
    /// <param name="orderConfiguration">订单实体构建器。</param>
    public void Configure(EntityTypeBuilder<Order> orderConfiguration)
    {
        // 映射到名为 "orders" 的数据库表
        orderConfiguration.ToTable("orders");

        // 忽略领域事件 (DomainEvents)，不需要持久化到数据库中
        orderConfiguration.Ignore(b => b.DomainEvents);

        // 主键 Id 使用 HiLo 序列生成策略，高低位序列名为 "orderseq"
        orderConfiguration.Property(o => o.Id)
            .UseHiLo("orderseq");

        // 将 Address 属性配置为所属实体类型 (Owned Entity Type)
        // 从 EF Core 2.0 开始，值对象 (Value Object) 推荐通过 OwnsOne 映射为数据库中的平铺列，避免创建单独的地址表
        orderConfiguration
            .OwnsOne(o => o.Address);

        // 将订单状态 OrderStatus 枚举在入库时转换为其字符串表达形式（例如 "Submitted", "Paid" 等），最大长度为 30
        orderConfiguration
            .Property(o => o.OrderStatus)
            .HasConversion<string>()
            .HasMaxLength(30);

        // 配置付款方式外键属性 PaymentId 的列名为 "PaymentMethodId"
        orderConfiguration
            .Property(o => o.PaymentId)
            .HasColumnName("PaymentMethodId");

        // 配置订单对支付方式的一对多导航关系（实际上是单向外键引用）
        // 限制删除行为 (DeleteBehavior.Restrict)，当支付方式被删除时不能删除级联订单
        orderConfiguration.HasOne<PaymentMethod>()
            .WithMany()
            .HasForeignKey(o => o.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        // 配置订单对买家 (Buyer) 的一对多关系，使用 BuyerId 作为外键
        orderConfiguration.HasOne(o => o.Buyer)
            .WithMany()
            .HasForeignKey(o => o.BuyerId);
    }
}
