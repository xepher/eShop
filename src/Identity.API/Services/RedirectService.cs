namespace eShop.Identity.API.Services
{
    /// <summary>
    /// 重定向服务实现类。
    /// 负责解析被编码的 returnUrl 并提取出真正的 OAuth/OIDC 客户端回调地址 redirect_uri。
    /// </summary>
    public class RedirectService : IRedirectService
    {
        /// <summary>
        /// 从包含多个参数的 returnUrl 中提取出真正的 redirect_uri，并对其进行 URL 解码。
        /// </summary>
        public string ExtractRedirectUriFromReturnUrl(string url)
        {
            // 对 HTML 编码的 URL 进行解码
            var decodedUrl = System.Net.WebUtility.HtmlDecode(url);
            
            // 按 "redirect_uri=" 切分字符串以定位重定向目标
            var results = Regex.Split(decodedUrl, "redirect_uri=");
            if (results.Length < 2)
                return "";

            string result = results[1];

            // 确定切分键。如果包含 signin-oidc（代表是标准的 OIDC 混合/授权码模式回调），
            // 则截取到 signin-oidc 为止；否则按 scope 截取
            string splitKey;
            if (result.Contains("signin-oidc"))
                splitKey = "signin-oidc";
            else
                splitKey = "scope";

            results = Regex.Split(result, splitKey);
            if (results.Length < 2)
                return "";

            result = results[0];

            // 拼接回完整的 URI 头部，并将 URL 编码的特殊字符还原，并移除多余的 & 符号
            return result.Replace("%3A", ":").Replace("%2F", "/").Replace("&", "");
        }
    }
}

