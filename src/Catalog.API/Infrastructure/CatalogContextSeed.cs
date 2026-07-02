using System.Text.Json;
using eShop.Catalog.API.Services;
using Pgvector;

namespace eShop.Catalog.API.Infrastructure;

/// <summary>
/// 商品目录数据库种子填充器（CatalogContextSeed），实现了 IDbSeeder 接口。
/// 当数据库启动或迁移完成时，自动运行此处的 SeedAsync 来导入初始化商品、品牌和类别数据，并预先计算 AI 向量数据。
/// </summary>
public partial class CatalogContextSeed(
    IWebHostEnvironment env,
    IOptions<CatalogOptions> settings,
    ICatalogAI catalogAI,
    ILogger<CatalogContextSeed> logger) : IDbSeeder<CatalogContext>
{
    /// <summary>
    /// 执行异步种子数据填充
    /// </summary>
    public async Task SeedAsync(CatalogContext context)
    {
        var useCustomizationData = settings.Value.UseCustomizationData;
        var contentRootPath = env.ContentRootPath;
        var picturePath = env.WebRootPath;

        // 规避机制 (Workaround)：在执行迁移后，需要手动打开连接并重新加载 PostgreSQL 类型定义，
        // 这样 Npgsql 驱动才能在运行时正确识别和映射 pgvector 字段类型。
        context.Database.OpenConnection();
        ((NpgsqlConnection)context.Database.GetDbConnection()).ReloadTypes();

        // 检查商品表是否为空，仅在无数据时才执行数据种子播种操作
        if (!context.CatalogItems.Any())
        {
            // 1. 读取 Setup 目录下的 catalog.json 初始化数据源
            var sourcePath = Path.Combine(contentRootPath, "Setup", "catalog.json");
            var sourceJson = File.ReadAllText(sourcePath);
            var sourceItems = JsonSerializer.Deserialize<CatalogSourceEntry[]>(sourceJson) ?? Array.Empty<CatalogSourceEntry>();

            // 2. 清空并重新生成商品品牌表 (CatalogBrands)
            context.CatalogBrands.RemoveRange(context.CatalogBrands);
            await context.CatalogBrands.AddRangeAsync(sourceItems.Select(x => x.Brand).Distinct()
                .Where(brandName => brandName != null)
                .Select(brandName => new CatalogBrand(brandName!)));
            logger.LogInformation("Seeded catalog with {NumBrands} brands", context.CatalogBrands.Count());

            // 3. 清空并重新生成商品类别表 (CatalogTypes)
            context.CatalogTypes.RemoveRange(context.CatalogTypes);
            await context.CatalogTypes.AddRangeAsync(sourceItems.Select(x => x.Type).Distinct()
                .Where(typeName => typeName != null)
                .Select(typeName => new CatalogType(typeName!)));
            logger.LogInformation("Seeded catalog with {NumTypes} types", context.CatalogTypes.Count());

            // 先行提交，确保数据库生成品牌和类别的自增 ID 标识
            await context.SaveChangesAsync();

            // 将数据库中已入库的品牌/类别名称与自增主键做内存映射，以方便后续组装商品实体
            var brandIdsByName = await context.CatalogBrands.ToDictionaryAsync(x => x.Brand, x => x.Id);
            var typeIdsByName = await context.CatalogTypes.ToDictionaryAsync(x => x.Type, x => x.Id);

            // 4. 构建初始化商品明细列表
            var catalogItems = sourceItems
                .Where(source => source.Name != null && source.Brand != null && source.Type != null)
                .Select(source => new CatalogItem(source.Name!)
            {
                Id = source.Id,
                Description = source.Description,
                Price = source.Price,
                CatalogBrandId = brandIdsByName[source.Brand!],
                CatalogTypeId = typeIdsByName[source.Type!],
                AvailableStock = 100,
                MaxStockThreshold = 200,
                RestockThreshold = 10,
                PictureFileName = $"{source.Id}.webp",
            }).ToArray();

            // 5. 关键 AI 特性：如果系统中配置并启用了向量嵌入引擎 (catalogAI.IsEnabled)
            // 则在此处预先计算并播种所有商品的语义向量特征，并写入商品的 Embedding 字段
            if (catalogAI.IsEnabled)
            {
                logger.LogInformation("Generating {NumItems} embeddings", catalogItems.Length);
                IReadOnlyList<Vector>? embeddings = await catalogAI.GetEmbeddingsAsync(catalogItems);
                for (int i = 0; i < catalogItems.Length; i++)
                {
                    catalogItems[i].Embedding = embeddings?[i];
                }
            }

            // 6. 将商品数据持久化到 PostgreSQL 数据库
            await context.CatalogItems.AddRangeAsync(catalogItems);
            logger.LogInformation("Seeded catalog with {NumItems} items", context.CatalogItems.Count());
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 用于解析 json 数据源的内部临时模型类
    /// </summary>
    private class CatalogSourceEntry
    {
        public int Id { get; set; }
        public string? Type { get; set; }
        public string? Brand { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
    }
}
