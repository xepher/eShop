namespace eShop.Identity.API.Services
{
    /// <summary>
    /// 自定义 IdentityServer 的 Profile 载入服务。
    /// 负责在生成令牌（ID Token / Access Token）时，从 ASP.NET Core Identity 数据库获取用户信息并转换、填充为自定义 Claim。
    /// </summary>
    public class ProfileService : IProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// 构造函数，注入用户管理器。
        /// </summary>
        public ProfileService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// 在获取用户 Profile 数据（颁发 Token / 访问 UserInfo 端点）时被调用。
        /// </summary>
        /// <param name="context">包含请求上下文和待填充的 IssuedClaims 列表。</param>
        /// <param name="cancellationToken">取消标记。</param>
        public async Task GetProfileDataAsync(ProfileDataRequestContext context, CancellationToken cancellationToken)
        {
            var subject = context.Subject ?? throw new ArgumentNullException(nameof(context.Subject));

            // 从主题声明中提取用户唯一标识符（sub）
            var subjectId = subject.Claims.Where(x => x.Type == "sub").FirstOrDefault()?.Value;

            // 在数据库中查找该用户
            var user = await _userManager.FindByIdAsync(subjectId);
            if (user == null)
                throw new ArgumentException("Invalid subject identifier");

            // 获取该用户关联的自定义 Claim 并添加到生成的令牌中
            var claims = GetClaimsFromUser(user);
            context.IssuedClaims = claims.ToList();
        }

        /// <summary>
        /// 检查当前用户是否仍然活跃（允许登录或刷新令牌）。
        /// </summary>
        /// <param name="context">包含激活状态结果的上下文。</param>
        /// <param name="cancellationToken">取消标记。</param>
        public async Task IsActiveAsync(IsActiveContext context, CancellationToken cancellationToken)
        {
            var subject = context.Subject ?? throw new ArgumentNullException(nameof(context.Subject));

            var subjectId = subject.Claims.Where(x => x.Type == "sub").FirstOrDefault()?.Value;
            var user = await _userManager.FindByIdAsync(subjectId);

            context.IsActive = false;

            if (user != null)
            {
                // 如果支持安全戳验证，检查当前 Token 的安全戳是否与数据库一致（防止更改密码后旧 Token 仍然有效）
                if (_userManager.SupportsUserSecurityStamp)
                {
                    var security_stamp = subject.Claims.Where(c => c.Type == "security_stamp").Select(c => c.Value).SingleOrDefault();
                    if (security_stamp != null)
                    {
                        var db_security_stamp = await _userManager.GetSecurityStampAsync(user);
                        if (db_security_stamp != security_stamp)
                            return; // 安全戳不匹配，不激活该用户
                    }
                }

                // 检查用户是否未被锁定（Lockout）
                context.IsActive =
                    !user.LockoutEnabled ||
                    !user.LockoutEnd.HasValue ||
                    user.LockoutEnd <= DateTime.UtcNow;
            }
        }

        /// <summary>
        /// 辅助方法：从 ApplicationUser 用户对象中提取并组装 Claims。
        /// 包含基本的 sub、name，以及购物车和订单所需的信用卡（只读前缀掩码）和收获地址信息。
        /// </summary>
        private IEnumerable<Claim> GetClaimsFromUser(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Subject, user.Id),
                new Claim(JwtClaimTypes.PreferredUserName, user.UserName),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName)
            };

            if (!string.IsNullOrWhiteSpace(user.Name))
                claims.Add(new Claim("name", user.Name));

            if (!string.IsNullOrWhiteSpace(user.LastName))
                claims.Add(new Claim("last_name", user.LastName));

            if (!string.IsNullOrWhiteSpace(user.CardNumber))
                claims.Add(new Claim("card_number", user.CardNumber));

            if (!string.IsNullOrWhiteSpace(user.CardHolderName))
                claims.Add(new Claim("card_holder", user.CardHolderName));

            if (!string.IsNullOrWhiteSpace(user.SecurityNumber))
                claims.Add(new Claim("card_security_number", user.SecurityNumber));

            if (!string.IsNullOrWhiteSpace(user.Expiration))
                claims.Add(new Claim("card_expiration", user.Expiration));

            if (!string.IsNullOrWhiteSpace(user.City))
                claims.Add(new Claim("address_city", user.City));

            if (!string.IsNullOrWhiteSpace(user.Country))
                claims.Add(new Claim("address_country", user.Country));

            if (!string.IsNullOrWhiteSpace(user.State))
                claims.Add(new Claim("address_state", user.State));

            if (!string.IsNullOrWhiteSpace(user.Street))
                claims.Add(new Claim("address_street", user.Street));

            if (!string.IsNullOrWhiteSpace(user.ZipCode))
                claims.Add(new Claim("address_zip_code", user.ZipCode));

            if (_userManager.SupportsUserEmail)
            {
                claims.AddRange(new[]
                {
                    new Claim(JwtClaimTypes.Email, user.Email),
                    new Claim(JwtClaimTypes.EmailVerified, user.EmailConfirmed ? "true" : "false", ClaimValueTypes.Boolean)
                });
            }

            if (_userManager.SupportsUserPhoneNumber && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                claims.AddRange(new[]
                {
                    new Claim(JwtClaimTypes.PhoneNumber, user.PhoneNumber),
                    new Claim(JwtClaimTypes.PhoneNumberVerified, user.PhoneNumberConfirmed ? "true" : "false", ClaimValueTypes.Boolean)
                });
            }

            return claims;
        }
    }
}

