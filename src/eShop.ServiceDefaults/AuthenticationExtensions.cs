using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace eShop.ServiceDefaults;

/// <summary>
/// 身份认证与授权相关的 ServiceCollection 扩展方法类。
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// 为微服务应用程序添加默认的 JWT Bearer 身份认证与授权服务。
    /// 读取配置中的 "Identity" 段，配置 Identity API 地址与受众受保护的 Scope/Audience。
    /// </summary>
    /// <param name="builder">HostApplicationBuilder 实例</param>
    /// <returns>依赖注入服务容器 (IServiceCollection)</returns>
    public static IServiceCollection AddDefaultAuthentication(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // 预期的配置格式示例：
        // {
        //   "Identity": {
        //     "Url": "http://identity",
        //     "Audience": "basket"
        //    }
        // }

        var identitySection = configuration.GetSection("Identity");

        // 如果配置中不存在 Identity 节点，则直接返回服务集合，不启用身份验证
        if (!identitySection.Exists())
        {
            return services;
        }

        // 默认情况下，.NET 的 JWT 处理器会将 "sub" 声明映射到标准的 ClaimTypes.NameIdentifier 上。
        // 为了保持跟原始令牌声明（OIDC 规范中的 sub）命名一致，在这里移除该内置映射。
        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

        // 启用认证并添加 JWT Bearer 验证处理程序
        services.AddAuthentication().AddJwtBearer(options =>
        {
            var identityUrl = identitySection.GetRequiredValue("Url");
            var audience = identitySection.GetRequiredValue("Audience");

            // 指定认证服务器地址，用于获取 /.well-known/openid-configuration 元数据
            options.Authority = identityUrl;
            // 本地开发测试环境可以关闭 HTTPS 元数据要求
            options.RequireHttpsMetadata = false;
            // 指定该微服务期望保护的 API 标识符 (Audience)
            options.Audience = audience;
            
#if DEBUG
            // 如果处于本地 DEBUG 开发模式，为了兼容 Android 模拟器访问本地主机的特殊 IP (10.0.2.2)，
            // 将其一同添加为允许的 JWT 签发者 (Issuers)。
            // 参见：https://learn.microsoft.com/en-us/dotnet/maui/data-cloud/local-web-services?view=net-maui-8.0#android
            options.TokenValidationParameters.ValidIssuers = [identityUrl, "https://10.0.2.2:5243"];
#else
            options.TokenValidationParameters.ValidIssuers = [identityUrl];
#endif
            
            // 在微服务架构中，可以通过更细粒度的 Scope 检查来授权，因此在这里全局禁用严格的 Audience 验证
            options.TokenValidationParameters.ValidateAudience = false;
        });

        // 注册 ASP.NET Core 的授权依赖服务（如 IAuthorizationService）
        services.AddAuthorization();

        return services;
    }
}
