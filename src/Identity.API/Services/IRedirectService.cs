namespace eShop.Identity.API.Services
{
    /// <summary>
    /// 重定向服务接口，用于处理和验证授权过程中的回调及重定向 URL。
    /// </summary>
    public interface IRedirectService
    {
        /// <summary>
        /// 从返回的 URL（如 returnUrl 查询参数）中提取实际的重定向回调 URI。
        /// </summary>
        /// <param name="url">包含重定向信息的源 URL 字符串。</param>
        /// <returns>提取并解码后的重定向回调 URI 字符串。</returns>
        string ExtractRedirectUriFromReturnUrl(string url);
    }
}

