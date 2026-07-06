// 创建 Web 应用程序构建器
var builder = WebApplication.CreateBuilder(args);

// 添加基础微服务默认配置（包含 OpenTelemetry 监控、健康检查等服务）
builder.AddServiceDefaults();

// 注册控制器和视图服务（Identity.API 提供了登录和同意授权的 MVC 页面）
builder.Services.AddControllersWithViews();

// 注册 PostgreSQL 数据库上下文（关联名称为 "identitydb" 的连接字符串）
builder.AddNpgsqlDbContext<ApplicationDbContext>("identitydb");

// Apply database migration automatically. Note that this approach is not
// recommended for production scenarios. Consider generating SQL scripts from
// migrations instead.
// 自动执行数据库迁移并执行种子数据播种（UsersSeed）
builder.Services.AddMigration<ApplicationDbContext, UsersSeed>();

// 配置 ASP.NET Core Identity，使用 ApplicationUser 作为用户模型，IdentityRole 作为角色模型
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>() // 指定 EF Core 作为存储介质
        .AddDefaultTokenProviders();                    // 注册默认令牌提供程序（如重置密码令牌等）

// 配置 Duende IdentityServer，作为 OIDC / OAuth 2.0 认证服务器
builder.Services.AddIdentityServer(options =>
{
    //options.IssuerUri = "null";
    options.Authentication.CookieLifetime = TimeSpan.FromHours(2); // 登录 Cookie 的有效生命周期为 2 小时

    // 启用各类型的审计事件上报
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;

    // TODO: Remove this line in production.
    options.KeyManagement.Enabled = false; // 开发环境下禁用秘钥管理，转而使用开发者临时秘钥
})
// 添加内存配置源（定义于 Config 类中）
.AddInMemoryIdentityResources(Config.GetResources())
.AddInMemoryApiScopes(Config.GetApiScopes())
.AddInMemoryApiResources(Config.GetApis())
.AddInMemoryClients(Config.GetClients(builder.Configuration))
// 将 ASP.NET Core Identity 集成到 IdentityServer
.AddAspNetIdentity<ApplicationUser>()
// TODO: Not recommended for production - you need to store your key material somewhere secure
// 添加开发者签名凭据（仅供开发测试使用，生产环境需要使用真实有效的证书证书）
.AddDeveloperSigningCredential();

// 注册自定义服务实例
builder.Services.AddTransient<IProfileService, ProfileService>();                  // 提供自定义 Token Claim 的 Profile 服务
builder.Services.AddTransient<ILoginService<ApplicationUser>, EFLoginService>();  // 封装 ASP.NET Core Identity 登录逻辑的服务
builder.Services.AddTransient<IRedirectService, RedirectService>();              // 登录/登出重定向校验服务

var app = builder.Build();

// 映射默认健康检查、度量指标等终结点
app.MapDefaultEndpoints();

// 启用静态文件访问（用于登录页面的 CSS、JS、图片等资源）
app.UseStaticFiles();

// This cookie policy fixes login issues with Chrome 80+ using HTTP
// 强制设置 SameSiteMode.Lax 以解决 Chrome 80+ 跨域或 HTTP 传输下的登录状态丢失问题
app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });

app.UseRouting();

// 启用 IdentityServer 中间件（处理授权、令牌颁发、用户信息访问等端点请求）
app.UseIdentityServer();

app.UseAuthorization();

// 映射默认的 MVC 路由（{controller=Home}/{action=Index}/{id?}）
app.MapDefaultControllerRoute();

// 启动应用程序
app.Run();

