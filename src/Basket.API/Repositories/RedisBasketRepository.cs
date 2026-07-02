using System.Text.Json.Serialization;
using eShop.Basket.API.Model;

namespace eShop.Basket.API.Repositories;

/// <summary>
/// 基于 Redis 存储实现的购物车仓储实现类。
/// 利用 StackExchange.Redis 执行高效的键值读写，并应用了 AOT 优化的 JSON 序列化。
/// </summary>
public class RedisBasketRepository(ILogger<RedisBasketRepository> logger, IConnectionMultiplexer redis) : IBasketRepository
{
    private readonly IDatabase _database = redis.GetDatabase();

    // 购物车存储方案：
    // - 每个唯一的购物车对应一个 Redis 字符串类型的 Key：/basket/{userId}
    private static RedisKey BasketKeyPrefix = "/basket/"u8.ToArray();
    // 特别说明：这里将前缀转换为 UTF8 byte 数组，能够避免运行时编码转换开销，提升 RedisClient 发送 Key 时的网络传输效率。

    /// <summary>
    /// 获取完整的 Redis 存储键
    /// </summary>
    private static RedisKey GetBasketKey(string userId) => BasketKeyPrefix.Append(userId);

    /// <summary>
    /// 异步删除 Redis 中的购物车
    /// </summary>
    public async Task<bool> DeleteBasketAsync(string id)
    {
        return await _database.KeyDeleteAsync(GetBasketKey(id));
    }

    /// <summary>
    /// 异步读取购物车
    /// </summary>
    public async Task<CustomerBasket?> GetBasketAsync(string customerId)
    {
        // 采用 StringGetLeaseAsync 租约获取方法，可以复用内部缓冲区以降低垃圾回收 (GC) 开销，非常适合高并发微服务场景
        using var data = await _database.StringGetLeaseAsync(GetBasketKey(customerId));

        if (data is null || data.Length == 0)
        {
            return null;
        }

        // 使用源生成生成的 JsonSerializerContext 反序列化二进制 JSON 数组，免去反射开销
        return JsonSerializer.Deserialize(data.Span, BasketSerializationContext.Default.CustomerBasket);
    }

    /// <summary>
    /// 异步更新购物车
    /// </summary>
    public async Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket basket)
    {
        // 1. 将模型序列化为 UTF8 字节数组
        var json = JsonSerializer.SerializeToUtf8Bytes(basket, BasketSerializationContext.Default.CustomerBasket);
        
        // 2. 将数据写入 Redis，不设过期时间（或者可在此处设定 TTL）
        var created = await _database.StringSetAsync(GetBasketKey(basket.BuyerId), json);

        if (!created)
        {
            logger.LogInformation("Problem occurred persisting the item.");
            return null;
        }

        logger.LogInformation("Basket item persisted successfully.");
        return await GetBasketAsync(basket.BuyerId);
    }
}

/// <summary>
/// AOT 友好的 JSON 序列化器上下文，用于编译时预生成 CustomerBasket 实体的序列化/反序列化元数据
/// </summary>
[JsonSerializable(typeof(CustomerBasket))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
public partial class BasketSerializationContext : JsonSerializerContext
{

}
