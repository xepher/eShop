namespace eShop.Ordering.Infrastructure.EntityConfigurations;

/// <summary>
/// 订单项实体 (OrderItem Entity，属于 Order 聚合内子实体) 的 EF Core 实体映射配置类。
/// </summary>
class OrderItemEntityTypeConfiguration
    : IEntityTypeConfiguration<OrderItem>
{
    /// <summary>
    /// 配置订单项实体的映射、主键序列生成规则以及影射影子外键属性。
    /// </summary>
    /// <param name="orderItemConfiguration">订单项实体构建器。</param>
    public void Configure(EntityTypeBuilder<OrderItem> orderItemConfiguration)
    {
        // 映射到名为 "orderItems" 的数据库表
        orderItemConfiguration.ToTable("orderItems");

        // 忽略领域事件 (DomainEvents)，不需要持久化
        orderItemConfiguration.Ignore(b => b.DomainEvents);

        // 主键 Id 使用 HiLo 序列生成策略，高低位序列名为 "orderitemseq"
        orderItemConfiguration.Property(o => o.Id)
            .UseHiLo("orderitemseq");

        // 配置影子属性 (Shadow Property) "OrderId"，表明此实体依赖于订单聚合根的 OrderId
        orderItemConfiguration.Property<int>("OrderId");
    }
}
