namespace eShop.EventBus.Events;

/// <summary>
/// 集成事件基类（使用 record 定义以获得不可变性、基于值的相等性及良好的序列化支持）。
/// 集成事件用于微服务/限界上下文之间的异步数据通信，以实现跨系统的最终一致性。
/// </summary>
public record IntegrationEvent
{
    /// <summary>
    /// 初始化集成事件，并生成事件的唯一标识和创建时间。
    /// </summary>
    public IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreationDate = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取或设置集成事件的唯一标识符。
    /// 使用 [JsonInclude] 保证其在序列化和反序列化时被正确处理。
    /// </summary>
    [JsonInclude]
    public Guid Id { get; set; }

    /// <summary>
    /// 获取或设置集成事件的创建时间（UTC）。
    /// 使用 [JsonInclude] 保证其在序列化和反序列化时被正确处理。
    /// </summary>
    [JsonInclude]
    public DateTime CreationDate { get; set; }
}

