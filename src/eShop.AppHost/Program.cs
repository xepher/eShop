using eShop.AppHost;

// 创建分布式应用程序生成器，用于定义和配置微服务之间的依赖关系和基础设施
var builder = DistributedApplication.CreateBuilder(args);

// 配置 Forwarded Headers 拦截器，确保反向代理后的原始请求头（如 Host, Schema, IP）能传递给后端服务
builder.AddForwardedHeaders();

// 声明并配置基础架构服务（容器化中间件）
// 1. Redis 容器：用于 Basket.API 的分布式缓存与购物车状态存储
var redis = builder.AddRedis("redis");

// 2. RabbitMQ 容器：充当微服务间的事件总线 (EventBus)，配置为持久化生命周期，防止容器重启导致数据丢失
var rabbitMq = builder.AddRabbitMQ("eventbus")
    .WithLifetime(ContainerLifetime.Persistent);

// 3. PostgreSQL 容器：使用支持 pgvector 扩展的镜像，以支撑 Catalog.API 的向量检索与语义搜索功能
var postgres = builder.AddPostgres("postgres")
    .WithImage("ankane/pgvector")
    .WithImageTag("latest")
    .WithLifetime(ContainerLifetime.Persistent);

// 在 PostgreSQL 实例中创建各自微服务独立的逻辑数据库（实现 Database-per-service 模式）
var catalogDb = postgres.AddDatabase("catalogdb");
var identityDb = postgres.AddDatabase("identitydb");
var orderDb = postgres.AddDatabase("orderingdb");
var webhooksDb = postgres.AddDatabase("webhooksdb");

// 根据环境决定使用 HTTP 还是 HTTPS 协议端口（CI 自动化测试如 Playwright 通常强制使用 HTTP 简化证书配置）
var launchProfileName = ShouldUseHttpForEndpoints() ? "http" : "https";

// ==========================================
// 核心微服务注册与配置 (Services)
// ==========================================

// 1. 认证授权服务 (Identity.API)
var identityApi = builder.AddProject<Projects.Identity_API>("identity-api", launchProfileName)
    .WithExternalHttpEndpoints() // 暴露外部终结点以供登录与令牌验证
    .WithReference(identityDb)   // 绑定 PostgreSQL 中的身份数据库
    .WithHttpHealthCheck("/health"); // 启用健康检查

// 获取 Identity 服务对应的终结点，供其他后端服务进行令牌校验或用户流向跳转
var identityEndpoint = identityApi.GetEndpoint(launchProfileName);

// 2. 购物车服务 (Basket.API)
var basketApi = builder.AddProject<Projects.Basket_API>("basket-api")
    .WithReference(redis)        // 依赖 Redis 缓存购物车数据
    .WithReference(rabbitMq).WaitFor(rabbitMq) // 依赖并等待 RabbitMQ 启动就绪后再启动，避免消息队列连接失败
    .WithEnvironment("Identity__Url", identityEndpoint); // 注入 Identity API 地址以支持基于 Token 的身份验证
redis.WithParentRelationship(basketApi); // 设置 Redis 与 Basket.API 的父子级关联，在 UI 树形展示中归类

// 3. 商品目录服务 (Catalog.API)
var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq) // 依赖并等待事件总线
    .WithReference(catalogDb);                 // 绑定目录数据库

// 4. 订单管理服务 (Ordering.API)
var orderingApi = builder.AddProject<Projects.Ordering_API>("ordering-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq) // 依赖并等待事件总线
    .WithReference(orderDb).WaitFor(orderDb)   // 依赖并等待订单数据库就绪
    .WithHttpHealthCheck("/health")
    .WithEnvironment("Identity__Url", identityEndpoint); // 注入 Identity 终结点

// 5. 订单处理后台任务 (OrderProcessor)
builder.AddProject<Projects.OrderProcessor>("order-processor")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(orderDb)
    .WaitFor(orderingApi); // 必须等待 orderingApi 启动完毕，因为数据库的 Entity Framework (EF) 迁移是在 orderingApi 启动时执行的

// 6. 支付处理后台任务 (PaymentProcessor)
builder.AddProject<Projects.PaymentProcessor>("payment-processor")
    .WithReference(rabbitMq).WaitFor(rabbitMq); // 订阅事件总线处理支付相关集成事件

// 7. Webhooks 触发集成服务 (Webhooks.API)
var webHooksApi = builder.AddProject<Projects.Webhooks_API>("webhooks-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(webhooksDb)
    .WithEnvironment("Identity__Url", identityEndpoint);

// ==========================================
// 边缘反向代理 (Reverse Proxy) 
// ==========================================
// 使用 YARP 配置移动端 BFF (Backend-For-Frontends)，统一路由入口，处理跨域和请求分发
builder.AddYarp("mobile-bff")
    .WithExternalHttpEndpoints()
    .ConfigureMobileBffRoutes(catalogApi, orderingApi, identityApi);

// ==========================================
// 前端客户端应用程序 (Apps)
// ==========================================

// 1. Webhook 管理后台 (WebhookClient)
var webhooksClient = builder.AddProject<Projects.WebhookClient>("webhooksclient", launchProfileName)
    .WithReference(webHooksApi)
    .WithEnvironment("IdentityUrl", identityEndpoint);

// 2. 主商城电商前端门户 (WebApp)
var webApp = builder.AddProject<Projects.WebApp>("webapp", launchProfileName)
    .WithExternalHttpEndpoints() // 允许外部公网/浏览器访问
    // 自定义前端在 Aspire Dashboard 上的显示名称和终结点链接
    .WithUrls(c => c.Urls.ForEach(u => u.DisplayText = $"Online Store ({u.Endpoint?.EndpointName})"))
    .WithReference(basketApi)
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WaitFor(identityApi) // 确保身份验证中心就绪后再加载前端以防止未授权错误
    .WithEnvironment("IdentityUrl", identityEndpoint);

// ==========================================
// 大语言模型 (LLM) 与向量检索集成
// ==========================================

// 是否启用 OpenAI 集成（用于商品搜索与推荐等智能场景）
bool useOpenAI = false;
if (useOpenAI)
{
    // 将 OpenAI 注入到 Catalog.API (处理向量嵌入) 和 WebApp (执行聊天/意图解析)
    builder.AddOpenAI(catalogApi, webApp, OpenAITarget.OpenAI); // 若使用 Azure OpenAI, 可更改为 OpenAITarget.AzureOpenAI
}

// 是否启用本地 Ollama 运行开源大模型
bool useOllama = false;
if (useOllama)
{
    builder.AddOllama(catalogApi, webApp);
}

// ==========================================
// 回调终结点配置 (Self-referencing & Cyclic references)
// ==========================================

// 注入前端各自的 CallBackUrl 环境变量，用于 OAuth2/OIDC 登录成功后的页面回调
webApp.WithEnvironment("CallBackUrl", webApp.GetEndpoint(launchProfileName));
webhooksClient.WithEnvironment("CallBackUrl", webhooksClient.GetEndpoint(launchProfileName));

// 解决循环依赖：Identity 服务需要知道所有客户端/后端 API 的回调地址（以完成授权重定向验证），
// 因此在所有应用节点分配好终结点后，将这些终结点地址通过环境变量一次性注入回 Identity.API。
identityApi.WithEnvironment("BasketApiClient", basketApi.GetEndpoint("http"))
           .WithEnvironment("OrderingApiClient", orderingApi.GetEndpoint("http"))
           .WithEnvironment("WebhooksApiClient", webHooksApi.GetEndpoint("http"))
           .WithEnvironment("WebhooksWebClient", webhooksClient.GetEndpoint(launchProfileName))
           .WithEnvironment("WebAppClient", webApp.GetEndpoint(launchProfileName));

// 构建并运行 Aspire 分布式应用
builder.Build().Run();

// 辅助方法：读取环境变量，指示是否在 CI 环境（如 Playwright 自动化测试流程中）强制所有终结点使用 HTTP。
static bool ShouldUseHttpForEndpoints()
{
    const string EnvVarName = "ESHOP_USE_HTTP_ENDPOINTS";
    var envValue = Environment.GetEnvironmentVariable(EnvVarName);

    // 如果环境变量存在且值等于 "1"，则返回 true
    return int.TryParse(envValue, out int result) && result == 1;
}
