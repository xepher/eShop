namespace eShop.Ordering.Infrastructure.EntityConfigurations;

/// <summary>
/// 买家聚合根 (Buyer Aggregate Root) 的 EF Core 实体映射配置类。
/// </summary>
class BuyerEntityTypeConfiguration
    : IEntityTypeConfiguration<Buyer>
{
    /// <summary>
    /// 配置买家实体的数据库表结构、主键生成策略、唯一索引及一对多关系。
    /// </summary>
    /// <param name="buyerConfiguration">买家实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<Buyer> buyerConfiguration)
    {
        // 映射到名为 "buyers" 的数据库表
        buyerConfiguration.ToTable("buyers");

        // 忽略领域事件 (DomainEvents) 属性，使其不映射到数据库列（领域事件仅在内存中处理）
        buyerConfiguration.Ignore(b => b.DomainEvents);

        // 主键 Id 使用 HiLo 序列生成策略，高低位序列名为 "buyerseq"
        buyerConfiguration.Property(b => b.Id)
            .UseHiLo("buyerseq");

        // 配置 IdentityGuid 字段最大长度为 200
        buyerConfiguration.Property(b => b.IdentityGuid)
            .HasMaxLength(200);

        // 为 IdentityGuid 创建唯一索引，以保证同一用户在系统中的唯一性
        buyerConfiguration.HasIndex("IdentityGuid")
            .IsUnique(true);

        // 配置与支付方式 (PaymentMethods) 的一对多关系
        // 一个买家可以有多种支付方式，关联关系在 PaymentMethod 端通过外键维护，此配置表明删除买家时需要级联删除或进行适当处理
        buyerConfiguration.HasMany(b => b.PaymentMethods)
            .WithOne();
    }
}
