using eShop.Catalog.API.Services;

/// <summary>
/// 商品目录微服务（Catalog.API）的扩展方法类，负责其特有依赖项的配置与服务注册。
/// </summary>
public static class Extensions
{
    /// <summary>
    /// 注册商品目录微服务的所有依赖服务
    /// </summary>
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        // 1. 特殊判定：如果是生成 OpenAPI JSON 的构建阶段 (Build-time OpenAPI generation)
        // 此时由于没有真实的外部数据库连接，我们只需要注册 DbContext 骨架类型，避免连接真实数据库引发异常导致编译报错。
        if (builder.Environment.IsBuild())
        {
            builder.Services.AddDbContext<CatalogContext>();
            return;
        }

        // 2. 注册并配置 PostgreSQL DbContext 连接，同时配置 Entity Framework 支持 pgvector 向量扩展
        builder.AddNpgsqlDbContext<CatalogContext>("catalogdb", configureDbContextOptions: dbContextOptionsBuilder =>
        {
            dbContextOptionsBuilder.UseNpgsql(npgsqlBuilder =>
            {
                // 启用 pgvector 向量支持，用于处理智能检索场景下的向量余弦相似度计算
                npgsqlBuilder.UseVector();
            });
        });

        // 3. 自动执行数据库迁移和测试数据填充（Seed）。
        // 提示：这对于开发环境下的快速跑通非常便利，但在生产环境建议通过专门的脚本或流水线控制。
        builder.Services.AddMigration<CatalogContext, CatalogContextSeed>();

        // 4. 注册 Outbox (发件箱) 模式所需的集成事件日志服务，保障数据库更新与事件发布之间的事务一致性
        builder.Services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<CatalogContext>>();

        // 5. 注册商品目录专用集成事件发布服务
        builder.Services.AddTransient<ICatalogIntegrationEventService, CatalogIntegrationEventService>();

        // 6. 注册事件总线，并订阅订单等待校验、已支付两个关键事件，以相应地调整商品库存
        builder.AddRabbitMqEventBus("eventbus")
               .AddSubscription<OrderStatusChangedToAwaitingValidationIntegrationEvent, OrderStatusChangedToAwaitingValidationIntegrationEventHandler>()
               .AddSubscription<OrderStatusChangedToPaidIntegrationEvent, OrderStatusChangedToPaidIntegrationEventHandler>();

        // 7. 强类型选项绑定配置：绑定 "CatalogOptions" 段配置（包含 PicBaseUrl 等）
        builder.Services.AddOptions<CatalogOptions>()
            .BindConfiguration(nameof(CatalogOptions));

        // 8. 配置大语言模型 (LLM) 商品向量嵌入（Embedding）生成器
        // 优先使用本地 Ollama 客户端，否则使用 OpenAI 客户端
        if (builder.Configuration["OllamaEnabled"] is string ollamaEnabled && bool.Parse(ollamaEnabled))
        {
            builder.AddOllamaApiClient("embedding")
                .AddEmbeddingGenerator();
        }
        else if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("textEmbeddingModel")))
        {
            builder.AddOpenAIClientFromConfiguration("textEmbeddingModel")
                .AddEmbeddingGenerator();
        }

        // 9. 注册商品目录智能搜索及 AI 处理相关服务 (CatalogAI)
        builder.Services.AddScoped<ICatalogAI, CatalogAI>();
    }
}
