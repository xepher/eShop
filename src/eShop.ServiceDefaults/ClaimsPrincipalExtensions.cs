using System.Security.Claims;

namespace eShop.ServiceDefaults;

/// <summary>
/// ClaimsPrincipal (用户安全主体) 扩展方法类，用于快速获取当前登录用户的常用声明信息。
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// 从当前用户的声明中提取唯一用户 ID（"sub" 声明通常在 OpenID Connect / OAuth2 中代表唯一标识符 Subject）。
    /// </summary>
    public static string? GetUserId(this ClaimsPrincipal principal)
        => principal.FindFirst("sub")?.Value;

    /// <summary>
    /// 从当前用户的声明中提取用户名（使用标准的 ClaimTypes.Name 声明）。
    /// </summary>
    public static string? GetUserName(this ClaimsPrincipal principal) =>
        principal.FindFirst(x => x.Type == ClaimTypes.Name)?.Value;
}
