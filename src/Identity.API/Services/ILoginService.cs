namespace eShop.Identity.API.Services
{
    /// <summary>
    /// 登录服务接口，提供统一的用户查询、密码验证及登录操作。
    /// </summary>
    /// <typeparam name="T">用户实体的类型。</typeparam>
    public interface ILoginService<T>
    {
        /// <summary>
        /// 验证用户凭据是否正确（匹配密码）。
        /// </summary>
        /// <param name="user">用户实体对象。</param>
        /// <param name="password">密码字符串。</param>
        /// <returns>返回凭据是否正确的布尔值任务。</returns>
        Task<bool> ValidateCredentials(T user, string password);

        /// <summary>
        /// 根据用户名（或电子邮件）查找用户。
        /// </summary>
        /// <param name="user">要查找的用户名或电子邮件。</param>
        /// <returns>返回找到的用户实体，若未找到则返回 null。</returns>
        Task<T> FindByUsername(string user);

        /// <summary>
        /// 执行用户常规登录操作。
        /// </summary>
        /// <param name="user">要登录的用户实体对象。</param>
        Task SignIn(T user);

        /// <summary>
        /// 执行带高级属性（如 Cookie 持久化配置、认证方法等）的登录操作。
        /// </summary>
        /// <param name="user">要登录的用户实体对象。</param>
        /// <param name="properties">ASP.NET Core 认证配置属性。</param>
        /// <param name="authenticationMethod">认证方式（如 OIDC、自定义方案等）。</param>
        Task SignInAsync(T user, AuthenticationProperties properties, string authenticationMethod = null);
    }
}

