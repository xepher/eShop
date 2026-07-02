using System.Text;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace eShop.ServiceDefaults;

/// <summary>
/// OpenAPI 配置生成选项的内部扩展类，用于微调生成的 OpenAPI 规范文档（如版本信息、安全机制、授权码匹配等）。
/// </summary>
internal static class OpenApiOptionsExtensions
{
    /// <summary>
    /// 为 OpenAPI 文档应用 API 版本详细信息（标题、版本号、描述信息，包括过时说明或日落计划信息）。
    /// </summary>
    public static OpenApiOptions ApplyApiVersionInfo(this OpenApiOptions options, string title, string description)
    {
        // 注册一个文档转换器 (Document Transformer) 来动态填充文档的 Info 字段
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            var versionedDescriptionProvider = context.ApplicationServices.GetService<IApiVersionDescriptionProvider>();
            var apiDescription = versionedDescriptionProvider?.ApiVersionDescriptions
                .SingleOrDefault(description => description.GroupName == context.DocumentName);
            if (apiDescription is null)
            {
                return Task.CompletedTask;
            }
            
            // 设置主版本号
            document.Info.Version = apiDescription.ApiVersion.ToString();
            // 设置标题
            document.Info.Title = title;
            // 组装并设置描述内容（包括版本是否废弃或已届 Sunset 期限）
            document.Info.Description = BuildDescription(apiDescription, description);
            return Task.CompletedTask;
        });
        return options;
    }

    /// <summary>
    /// 辅助方法：构建版本相关的 API 描述文档，自动追加 API 过期废弃 (Deprecated) 及日落宣告 (Sunset) 政策提示。
    /// </summary>
    private static string BuildDescription(ApiVersionDescription api, string description)
    {
        var text = new StringBuilder(description);

        // 如果该版本已被标记为过时废弃
        if (api.IsDeprecated)
        {
            if (text.Length > 0)
            {
                if (text[^1] != '.')
                {
                    text.Append('.');
                }

                text.Append(' ');
            }

            text.Append("This API version has been deprecated.");
        }

        // 如果存在 Sunset (日落) 下线宣告政策，在文档中提供对应下线时间和参考链接
        if (api.SunsetPolicy is { } policy)
        {
            if (policy.Date is { } when)
            {
                if (text.Length > 0)
                {
                    text.Append(' ');
                }

                text.Append("The API will be sunset on ")
                    .Append(when.Date.ToShortDateString())
                    .Append('.');
            }

            // 渲染日落指南等关联链接
            if (policy.HasLinks)
            {
                text.AppendLine();

                var rendered = false;

                foreach (var link in policy.Links.Where(l => l.Type == "text/html"))
                {
                    if (!rendered)
                    {
                        text.Append("<h4>Links</h4><ul>");
                        rendered = true;
                    }

                    text.Append("<li><a href=\"");
                    text.Append(link.LinkTarget.OriginalString);
                    text.Append("\">");
                    text.Append(
                        StringSegment.IsNullOrEmpty(link.Title)
                        ? link.LinkTarget.OriginalString
                        : link.Title.ToString());
                    text.Append("</a></li>");
                }

                if (rendered)
                {
                    text.Append("</ul>");
                }
            }
        }

        return text.ToString();
    }

    /// <summary>
    /// 应用全局安全机制方案定义，例如 OAuth2 安全流程。
    /// </summary>
    public static OpenApiOptions ApplySecuritySchemeDefinitions(this OpenApiOptions options)
    {
        options.AddDocumentTransformer<SecuritySchemeDefinitionsTransformer>();
        return options;
    }

    /// <summary>
    /// 应用操作级授权检查。
    /// 如果某个 API 终结点标有 [Authorize] 或实现了 IAuthorizeData 接口，
    /// 则自动在 OpenAPI 规范中将该操作标记为安全受限接口，需要 OAuth2 认证，并自动追加 401 和 403 状态码说明。
    /// </summary>
    public static OpenApiOptions ApplyAuthorizationChecks(this OpenApiOptions options, string[] scopes)
    {
        // 注册一个操作转换器 (Operation Transformer)
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            // 获取接口元数据
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;

            // 如果该接口不需要授权 (未加 [Authorize] 等特征)，则无需处理
            if (!metadata.OfType<IAuthorizeData>().Any())
            {
                return Task.CompletedTask;
            }

            // 初始化或获取响应字典，并加入 401(Unauthorized) & 403(Forbidden) 响应说明
            operation.Responses ??= new OpenApiResponses();
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

            // 引用我们在文档级别定义好的 "oauth2" 安全框架组件
            var oAuthScheme = new OpenApiSecuritySchemeReference("oauth2", null);

            // 将此 OAuth2 认证规则以及所需的作用域范围 (Scopes) 绑定到此操作描述中
            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new()
                {
                    [oAuthScheme] = scopes.ToList()
                }
            };

            return Task.CompletedTask;
        });
        return options;
    }

    /// <summary>
    /// 操作转换器：自动检测 API 操作方法或类上的 [Obsolete] 废弃特性，并同步更新 OpenAPI 描述文件中的 deprecated 状态。
    /// </summary>
    public static OpenApiOptions ApplyOperationDeprecatedStatus(this OpenApiOptions options)
    {
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            operation.Deprecated = operation.Deprecated || context.Description.ActionDescriptor.EndpointMetadata
                .OfType<ObsoleteAttribute>()
                .Any();

            return Task.CompletedTask;
        });
        return options;
    }

    /// <summary>
    /// 操作转换器：针对 API 版本控制参数 "api-version" 进行体验优化。
    /// 自动将默认值替换为示例值，并隐藏默认值字段，以防止前端代码生成工具或 Swagger/Scalar UI 预填充默认值导致非预期传参。
    /// </summary>
    public static OpenApiOptions ApplyApiVersionDescription(this OpenApiOptions options)
    {
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            // 查找到请求路径中的 api-version 参数
            var apiVersionParameter = operation.Parameters?.FirstOrDefault(p => p.Name == "api-version");
            if (apiVersionParameter?.Schema is OpenApiSchema targetSchema)
            {
                // 将默认值作为 Example 示例展示，并清空默认值定义
                targetSchema.Example = targetSchema.Default;
                targetSchema.Default = null;
            }
            return Task.CompletedTask;
        });
        return options;
    }

    /// <summary>
    /// 自定义 OpenAPI 文档转换器，负责将 Identity 服务的配置安全定义（OAuth2）写入 OpenAPI 规范文档。
    /// </summary>
    private class SecuritySchemeDefinitionsTransformer(IConfiguration configuration) : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var identitySection = configuration.GetSection("Identity");
            // 如果未配置身份认证中心，跳过安全组件声明
            if (!identitySection.Exists())
            {
                return Task.CompletedTask;
            }

            var identityUrlExternal = identitySection.GetRequiredValue("Url");
            // 获取所需的 Scope 字典映射，用于授权界面用户勾选
            var scopes = identitySection.GetRequiredSection("Scopes").GetChildren().ToDictionary(p => p.Key, p => p.Value ?? string.Empty);
            
            // 定义 OAuth2 安全框架描述，这里使用的是 Implicit (隐式) 流。
            // TODO: 后期建议升级配置为安全性更高的具有 PKCE 机制的 Authorization Code 授权码流。
            var securityScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows()
                {
                    Implicit = new OpenApiOAuthFlow()
                    {
                        AuthorizationUrl = new Uri($"{identityUrlExternal}/connect/authorize"),
                        TokenUrl = new Uri($"{identityUrlExternal}/connect/token"),
                        Scopes = scopes,
                    }
                }
            };

            // 将安全模型声明加入到 OpenAPI Components 库中，命名为 "oauth2"
            document.Components ??= new();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes.Add("oauth2", securityScheme);
            return Task.CompletedTask;
        }
    }
}
