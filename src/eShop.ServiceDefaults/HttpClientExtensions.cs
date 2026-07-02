using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication;

namespace eShop.ServiceDefaults;

/// <summary>
/// HttpClient 扩展方法类，用于配置跨微服务调用时的身份令牌传播。
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// 为 HttpClient 绑定认证令牌传播处理器 (HttpClientAuthorizationDelegatingHandler)。
    /// 当当前 HTTP 请求上下文 (HttpContext) 中存在 JWT Access Token 时，此处理器会自动将其附加到外出的 HTTP 请求头中（作为 Bearer 令牌）。
    /// </summary>
    /// <param name="builder">HttpClient 生成器</param>
    /// <returns>HttpClient 生成器以支持链式调用</returns>
    public static IHttpClientBuilder AddAuthToken(this IHttpClientBuilder builder)
    {
        // 注册 HttpContextAccessor，以便在代理处理器中访问当前 HTTP 请求的上下文
        builder.Services.AddHttpContextAccessor();

        // 注册专用的委托处理器 (DelegatingHandler)
        builder.Services.TryAddTransient<HttpClientAuthorizationDelegatingHandler>();

        // 将该委托处理器注入到 HttpClient 请求管道中
        builder.AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>();

        return builder;
    }

    /// <summary>
    /// 自定义 HTTP 消息处理委托器，实现身份令牌的级联传递 (Token Propagation)。
    /// </summary>
    private class HttpClientAuthorizationDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpClientAuthorizationDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public HttpClientAuthorizationDelegatingHandler(IHttpContextAccessor httpContextAccessor, HttpMessageHandler innerHandler) : base(innerHandler)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 拦截外出的 HTTP 请求，并向其中注入当前用户的 JWT 授权头。
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 如果存在活动中的 HttpContext，则尝试获取当前用户的 Access Token
            if (_httpContextAccessor.HttpContext is HttpContext context)
            {
                // 从当前请求的认证信息中提取 "access_token"
                var accessToken = await context.GetTokenAsync("access_token");

                // 如果 token 存在，将其作为 Bearer Token 写入传出请求的 Authorization 头中
                if (accessToken is not null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
            }

            // 继续执行请求处理管道
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
