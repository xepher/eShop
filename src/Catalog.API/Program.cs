// 创建 Web 应用程序生成器，用于构建 ASP.NET Core 微服务宿主
var builder = WebApplication.CreateBuilder(args);

// 注册 .NET Aspire 标准服务组件（包括 OpenTelemetry, 服务发现, 默认客户端弹性重试等）
builder.AddServiceDefaults();

// 注册商品目录微服务（Catalog.API）特有的应用级服务（包括数据库连接、AI 向量检索、后台数据库播种迁移等）
builder.AddApplicationServices();

// 注册内置错误详情处理服务 (ProblemDetails)，支持标准化 RFC 7807 错误响应格式
builder.Services.AddProblemDetails();

// 配置微服务的 API 版本控制（API Versioning）策略
var withApiVersioning = builder.Services.AddApiVersioning(options =>
{
    // 在 HTTP 响应头中自动附加支持的 API 版本（"api-supported-versions"）和已废弃的版本（"api-deprecated-versions"）
    options.ReportApiVersions = true;
});

// 配置默认的 OpenAPI (Swagger/Scalar) 生成选项并与 API 版本控制相结合
builder.AddDefaultOpenApi(withApiVersioning);

// 构建 Web 应用程序实例
var app = builder.Build();

// 映射默认的健康检查及诊断路由 (来自 ServiceDefaults)
app.MapDefaultEndpoints();

// 启用标准化 HTTP 状态码响应页面中间件
app.UseStatusCodePages();

// 映射基于 Minimal APIs 构建的商品目录控制器终结点路由（CatalogApi）
app.MapCatalogApi();

// 启用并映射默认的 OpenAPI / Scalar API 交互界面中间件
app.UseDefaultOpenApi();

// 启动商品目录微服务
app.Run();
