using System.Text.Json.Serialization;
using eShop.Basket.API.Repositories;
using eShop.Basket.API.IntegrationEvents.EventHandling;
using eShop.Basket.API.IntegrationEvents.EventHandling.Events;

namespace eShop.Basket.API.Extensions;

/// <summary>
/// 购物车服务的依赖注入服务注册扩展类
/// </summary>
public static class Extensions
{
    /// <summary>
    /// 注册购物车应用所需的核心依赖服务
    /// </summary>
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        // 1. 注册默认身份验证服务（支持 JWT Token 校验）
        builder.AddDefaultAuthentication();

        // 2. 注册 Redis 客户端依赖，利用 Aspire 的集成组件连接命名为 "redis" 的 Redis 服务
        builder.AddRedisClient("redis");

        // 3. 注册购物车仓储实例，采用单例模式管理（Redis 客户端内部已实现线程安全的连接复用）
        builder.Services.AddSingleton<IBasketRepository, RedisBasketRepository>();

        // 4. 注册 RabbitMQ 事件总线，订阅订单创建启动事件（OrderStartedIntegrationEvent）
        // 并配置 JSON 序列化器源生成上下文以提供卓越的高性能 AOT/反射优化
        builder.AddRabbitMqEventBus("eventbus")
               .AddSubscription<OrderStartedIntegrationEvent, OrderStartedIntegrationEventHandler>()
               .ConfigureJsonOptions(options => options.TypeInfoResolverChain.Add(IntegrationEventContext.Default));
    }
}

/// <summary>
/// AOT 友好的 JSON 序列化器上下文，用于编译时预生成集成事件的序列化/反序列化元数据
/// </summary>
[JsonSerializable(typeof(OrderStartedIntegrationEvent))]
partial class IntegrationEventContext : JsonSerializerContext
{

}
