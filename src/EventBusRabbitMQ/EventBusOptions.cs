namespace eShop.EventBusRabbitMQ;

/// <summary>
/// 事件总线配置选项类，用于配置 RabbitMQ 相关的连接和消费行为。
/// </summary>
public class EventBusOptions
{
    /// <summary>
    /// 获取或设置订阅客户端名称（通常对应 RabbitMQ 的队列名称，用于实现发布/订阅模型下的竞争消费者模式）。
    /// </summary>
    public string SubscriptionClientName { get; set; }

    /// <summary>
    /// 获取或设置发布或操作失败时的重试次数，默认值为 10 次。
    /// </summary>
    public int RetryCount { get; set; } = 10;
}

