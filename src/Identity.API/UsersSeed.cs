
namespace eShop.Identity.API;

/// <summary>
/// 数据库种子数据播种器，用于在数据库初始化或迁移时自动填充默认测试用户（alice 和 bob）。
/// </summary>
public class UsersSeed(ILogger<UsersSeed> logger, UserManager<ApplicationUser> userManager) : IDbSeeder<ApplicationDbContext>
{
    /// <summary>
    /// 异步播种种子数据。
    /// </summary>
    /// <param name="context">Identity 数据库上下文。</param>
    /// <returns>表示异步操作的任务 Task。</returns>
    public async Task SeedAsync(ApplicationDbContext context)
    {
        // 尝试查找默认用户 "alice"
        var alice = await userManager.FindByNameAsync("alice");

        if (alice == null)
        {
            // 构造默认测试用户 alice 的各项属性（包含模拟信用卡和送货地址信息）
            alice = new ApplicationUser
            {
                UserName = "alice",
                Email = "AliceSmith@email.com",
                EmailConfirmed = true,
                CardHolderName = "Alice Smith",
                CardNumber = "XXXXXXXXXXXX1881",
                CardType = 1,
                City = "Redmond",
                Country = "U.S.",
                Expiration = "12/24",
                Id = Guid.NewGuid().ToString(),
                LastName = "Smith",
                Name = "Alice",
                PhoneNumber = "1234567890",
                ZipCode = "98052",
                State = "WA",
                Street = "15703 NE 61st Ct",
                SecurityNumber = "123"
            };

            // 创建用户并设置默认密码 "Pass123$"
            var result = await userManager.CreateAsync(alice, "Pass123$");

            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("alice created");
            }
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("alice already exists");
            }
        }

        // 尝试查找默认用户 "bob"
        var bob = await userManager.FindByNameAsync("bob");

        if (bob == null)
        {
            // 构造默认测试用户 bob 的各项属性
            bob = new ApplicationUser
            {
                UserName = "bob",
                Email = "BobSmith@email.com",
                EmailConfirmed = true,
                CardHolderName = "Bob Smith",
                CardNumber = "XXXXXXXXXXXX1881",
                CardType = 1,
                City = "Redmond",
                Country = "U.S.",
                Expiration = "12/24",
                Id = Guid.NewGuid().ToString(),
                LastName = "Smith",
                Name = "Bob",
                PhoneNumber = "1234567890",
                ZipCode = "98052",
                State = "WA",
                Street = "15703 NE 61st Ct",
                SecurityNumber = "456"
            };

            // 创建用户并设置默认密码 "Pass123$"
            var result = await userManager.CreateAsync(bob, "Pass123$");

            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("bob created");
            }
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("bob already exists");
            }
        }
    }
}

