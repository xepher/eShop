using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace eShop.ServiceDefaults;

/// <summary>
/// OpenAPI (Swagger/Scalar) 以及 API 版本控制相关的扩展方法类。
/// </summary>
public static partial class Extensions
{
    /// <summary>
    /// 注册与配置 OpenAPI 的默认中间件（在应用程序流水线中）。
    /// 如果配置中未指定 "OpenApi" 段，则直接跳过配置。
    /// 在开发环境中自动配置并启用基于 Scalar 的现代 API 交互式文档界面，并设置根路径的自动重定向。
    /// </summary>
    /// <param name="app">WebApplication 实例</param>
    /// <returns>WebApplication 实例支持链式调用</returns>
    public static IApplicationBuilder UseDefaultOpenApi(this WebApplication app)
    {
        var configuration = app.Configuration;
        var openApiSection = configuration.GetSection("OpenApi");

        // 如果没有 OpenApi 配置项，则不注册 OpenAPI 终结点和 UI
        if (!openApiSection.Exists())
        {
            return app;
        }

        // 注册内置 OpenAPI 元数据终结点，并配置按 API 版本生成不同的 OpenAPI 文档
        app.MapOpenApi().WithDocumentPerVersion();

        // 仅在本地开发环境中暴露 Scalar API 文档 UI
        if (app.Environment.IsDevelopment())
        {
            // 获取所有的 API 版本描述信息
            var descriptions = app.DescribeApiVersions();
            // 设定最新版本（列表中的最后一个）为默认展示的文档版本，无描述则默认 v1
            var defaultDocument = descriptions.Count > 0 ? descriptions[^1].GroupName : "v1";

            // 映射并配置 Scalar API 文档 UI 终结点
            app.MapScalarApiReference(options =>
            {
                // 禁用 Scalar 的默认第三方字体，以避免离线状态下字体下载受阻，并且提高页面加载性能
                options.DefaultFonts = false;

                // 为每个检测到的 API 版本添加一个独立的 OpenAPI 文档选项卡
                foreach (var description in descriptions)
                {
                    options.AddDocument(description.GroupName, description.GroupName, isDefault: description.GroupName == defaultDocument);
                }
            });

            // 当访问站点根路径时，自动重定向到最新的 Scalar API 文档页面，并从 API 文档本身排除此路由
            app.MapGet("/", () => Results.Redirect($"/scalar/{defaultDocument}")).ExcludeFromDescription();
        }

        return app;
    }

    /// <summary>
    /// 在服务依赖注入容器中添加并配置 API 版本管理 (API Versioning) 和 OpenAPI 文档生成的底层设置。
    /// 会从配置中提取 "OpenApi" 和 "Identity" 部分来注入 OAuth 授权作用域 (Scopes)。
    /// </summary>
    /// <param name="builder">HostApplicationBuilder 实例</param>
    /// <param name="apiVersioning">API 版本控制生成器，如果为 null 则不配置版本化文档</param>
    /// <returns>HostApplicationBuilder 实例</returns>
    public static IHostApplicationBuilder AddDefaultOpenApi(
        this IHostApplicationBuilder builder,
        IApiVersioningBuilder? apiVersioning = default)
    {
        var openApi = builder.Configuration.GetSection("OpenApi");
        var identitySection = builder.Configuration.GetSection("Identity");

        // 提取配置中配置的 Scopes（权限范围），作为 OpenAPI 文档的安全定义作用域
        var scopes = identitySection.Exists()
            ? identitySection.GetRequiredSection("Scopes").GetChildren().ToDictionary(p => p.Key, p => p.Value)
            : new Dictionary<string, string?>();

        // 如果没有 OpenApi 配置，则不执行后续 OpenAPI 配置逻辑
        if (!openApi.Exists())
        {
            return builder;
        }

        if (apiVersioning is not null)
        {
            // 默认的 API 版本格式是 ApiVersion.ToString()，例如 "1.0"。
            // 这里的配置会将其格式化为 "'v'major[.minor][-status]" 形式（例如 "v1", "v2"）。
            apiVersioning.AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV"; // 'v' + 主版本号+次版本号+修饰状态
                    options.DefaultApiVersionParameterDescription = "The API version, in the format 'major.minor'.";
                })
                // 集成并添加 Microsoft.AspNetCore.OpenApi 的默认支持
                .AddOpenApi(options =>
                {
                    var document = options.Document;

                    // 1. 应用基本文档信息（标题和描述）
                    document.ApplyApiVersionInfo(openApi.GetRequiredValue("Document:Title"), openApi.GetRequiredValue("Document:Description"));
                    // 2. 检查控制器或操作方法上的 [Authorize] 标记，自动添加 401/403 响应和 OAuth 安全校验组件
                    document.ApplyAuthorizationChecks([.. scopes.Keys]);
                    // 3. 注册 OAuth2 隐式流安全方案定义（包括 authorize/token 端点）
                    document.ApplySecuritySchemeDefinitions();
                    // 4. 将标记了 [Obsolete] 特性的 API 接口在文档中标记为废弃 (Deprecated)
                    document.ApplyOperationDeprecatedStatus();
                    // 5. 调整文档中 api-version 参数的展现方式
                    document.ApplyApiVersionDescription();
                });
        }

        return builder;
    }
}
