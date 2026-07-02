using System.Security.Claims;
using Grpc.Core;

namespace eShop.Basket.API.Extensions;

/// <summary>
/// gRPC 调用上下文 (ServerCallContext) 的身份扩展方法类，用于直接从 gRPC HTTP 上下文中获取调用方身份。
/// </summary>
internal static class ServerCallContextIdentityExtensions
{
    /// <summary>
    /// 从 gRPC 调用关联的 HttpContext 中提取用户 ID（OIDC 中的 "sub" 声明）
    /// </summary>
    public static string? GetUserIdentity(this ServerCallContext context) => context.GetHttpContext().User.FindFirst("sub")?.Value;

    /// <summary>
    /// 从 gRPC 调用关联的 HttpContext 中提取用户名
    /// </summary>
    public static string? GetUserName(this ServerCallContext context) => context.GetHttpContext().User.FindFirst(x => x.Type == ClaimTypes.Name)?.Value;
}
