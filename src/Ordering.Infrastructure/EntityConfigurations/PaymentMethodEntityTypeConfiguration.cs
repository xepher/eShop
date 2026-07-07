namespace eShop.Ordering.Infrastructure.EntityConfigurations;

/// <summary>
/// 支付方式实体 (PaymentMethod Entity，属于 Buyer 聚合) 的 EF Core 实体映射配置类。
/// </summary>
class PaymentMethodEntityTypeConfiguration
    : IEntityTypeConfiguration<PaymentMethod>
{
    /// <summary>
    /// 配置支付方式实体的映射关系。由于 DDD 中强调强封装性，许多内部属性仅公开私有字段形式存在，
    /// 这里利用 EF Core 配置私有字段映射至数据库列的能力。
    /// </summary>
    /// <param name="paymentConfiguration">支付方式实体构建器。</param>
    public void Configure(EntityTypeBuilder<PaymentMethod> paymentConfiguration)
    {
        // 映射到名为 "paymentmethods" 的数据库表
        paymentConfiguration.ToTable("paymentmethods");

        // 忽略领域事件，仅在内存中分发
        paymentConfiguration.Ignore(b => b.DomainEvents);

        // 主键 Id 使用 HiLo 序列生成策略，高低位序列名为 "paymentseq"
        paymentConfiguration.Property(b => b.Id)
            .UseHiLo("paymentseq");

        // 配置影子外键属性 "BuyerId"，用来建立与买家实体的一对多关联关系
        paymentConfiguration.Property<int>("BuyerId");

        // 配置私有字段 _cardHolderName 映射到数据库列 CardHolderName，长度最大为 200
        paymentConfiguration
            .Property("_cardHolderName")
            .HasColumnName("CardHolderName")
            .HasMaxLength(200);

        // 配置私有字段 _alias 映射到数据库列 Alias，长度最大为 200
        paymentConfiguration
            .Property("_alias")
            .HasColumnName("Alias")
            .HasMaxLength(200);

        // 配置私有字段 _cardNumber 映射到数据库列 CardNumber，长度最大为 25 且必填
        paymentConfiguration
            .Property("_cardNumber")
            .HasColumnName("CardNumber")
            .HasMaxLength(25)
            .IsRequired();

        // 配置私有字段 _expiration 映射到数据库列 Expiration，长度最大为 25
        paymentConfiguration
            .Property("_expiration")
            .HasColumnName("Expiration")
            .HasMaxLength(25);

        // 配置私有外键字段 _cardTypeId 映射到数据库列 CardTypeId
        paymentConfiguration
            .Property("_cardTypeId")
            .HasColumnName("CardTypeId");

        // 配置与卡片类型 (CardType) 的导航关系，使用私有外键字段 _cardTypeId 进行关联
        paymentConfiguration.HasOne(p => p.CardType)
            .WithMany()
            .HasForeignKey("_cardTypeId");
    }
}
