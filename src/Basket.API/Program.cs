// 创建 Web 应用程序生成器，用于构建 ASP.NET Core 宿主
var builder = WebApplication.CreateBuilder(args);

// 注册基础服务配置（包括健康检查、可观测性 OpenTelemetry 等）
builder.AddBasicServiceDefaults();

// 注册 Basket.API 特有的应用级服务（包括 Redis 客户端、Repository、RabbitMQ 事件总线等）
builder.AddApplicationServices();

// 注册 gRPC 服务，使微服务支持高性能 RPC 远程调用
builder.Services.AddGrpc();

// 构建 Web 应用程序实例
var app = builder.Build();

// 映射 Aspire 的默认终结点（健康检查、存活接口等）
app.MapDefaultEndpoints();

// 映射 gRPC 服务实现，将端点与 BasketService 关联
app.MapGrpcService<BasketService>();

// 启动并运行应用程序
app.Run();
