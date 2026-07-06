namespace eShop.Identity.API.Services
{
    /// <summary>
    /// 基于 Entity Framework Core 成员管理（ASP.NET Core Identity）的登录服务实现类。
    /// </summary>
    public class EFLoginService : ILoginService<ApplicationUser>
    {
        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _signInManager;

        /// <summary>
        /// 构造函数，注入 ASP.NET Core Identity 相关的管理器。
        /// </summary>
        public EFLoginService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// 根据电子邮箱（用户名）查找 ApplicationUser。
        /// </summary>
        public async Task<ApplicationUser> FindByUsername(string user)
        {
            return await _userManager.FindByEmailAsync(user);
        }

        /// <summary>
        /// 验证密码是否正确。
        /// </summary>
        public async Task<bool> ValidateCredentials(ApplicationUser user, string password)
        {
            return await _userManager.CheckPasswordAsync(user, password);
        }

        /// <summary>
        /// 常规登录（持久化为永不过期的持久 Cookie，默认设置为 true）。
        /// </summary>
        public Task SignIn(ApplicationUser user)
        {
            return _signInManager.SignInAsync(user, true);
        }

        /// <summary>
        /// 使用提供的认证配置信息和认证方式进行登录。
        /// </summary>
        public Task SignInAsync(ApplicationUser user, AuthenticationProperties properties, string authenticationMethod = null)
        {
            return _signInManager.SignInAsync(user, properties, authenticationMethod);
        }
    }
}

