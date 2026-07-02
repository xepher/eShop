namespace eShop.Catalog.API.Infrastructure;

/// <summary>
/// 商品目录服务的 Entity Framework Core 数据库上下文（CatalogContext）。
/// </summary>
/// <remarks>
/// 如何在此目录下创建 EF 数据库迁移：
/// dotnet ef migrations add --context CatalogContext [migration-name]
/// </remarks>
public class CatalogContext : DbContext
{
    public CatalogContext(DbContextOptions<CatalogContext> options, IConfiguration configuration) : base(options)
    {
    }

    /// <summary>
    /// 商品项目表
    /// </summary>
    public required DbSet<CatalogItem> CatalogItems { get; set; }

    /// <summary>
    /// 商品品牌表
    /// </summary>
    public required DbSet<CatalogBrand> CatalogBrands { get; set; }

    /// <summary>
    /// 商品类别表
    /// </summary>
    public required DbSet<CatalogType> CatalogTypes { get; set; }

    /// <summary>
    /// 数据库模型关系创建映射配置
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // 1. 注册 PostgreSQL 的 pgvector（向量扩展），用以存储和检索商品的 Embedding 字段
        builder.HasPostgresExtension("vector");

        // 2. 依次加载商品品牌、类别和商品主表的 Fluent API 属性配置类
        builder.ApplyConfiguration(new CatalogBrandEntityTypeConfiguration());
        builder.ApplyConfiguration(new CatalogTypeEntityTypeConfiguration());
        builder.ApplyConfiguration(new CatalogItemEntityTypeConfiguration());

        // 3. 注册内置的事件日志发件箱（Outbox）表，确保本地业务事务与集成事件日志落库保持在同一个数据库事务内
        builder.UseIntegrationEventLogs();
    }
}
