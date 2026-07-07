namespace eShop.Ordering.Infrastructure.EntityConfigurations;

/// <summary>
/// 卡片类型 (CardType 值对象/静态实体) 的 EF Core 实体映射配置类。
/// </summary>
class CardTypeEntityTypeConfiguration
    : IEntityTypeConfiguration<CardType>
{
    /// <summary>
    /// 配置卡片类型实体的映射关系。
    /// </summary>
    /// <param name="cardTypesConfiguration">卡片类型实体构建器。</param>
    public void Configure(EntityTypeBuilder<CardType> cardTypesConfiguration)
    {
        // 映射到名为 "cardtypes" 的数据库表
        cardTypesConfiguration.ToTable("cardtypes");

        // 显式指定主键 Id 的值不需要在数据库端自动生成（CardType 作为枚举类型的实体，其 ID 为静态的常量）
        cardTypesConfiguration.Property(ct => ct.Id)
            .ValueGeneratedNever();

        // 卡片名称 Name，最大长度为 200，且为必填字段
        cardTypesConfiguration.Property(ct => ct.Name)
            .HasMaxLength(200)
            .IsRequired();
    }
}
