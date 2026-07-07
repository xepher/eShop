namespace eShop.IntegrationEventLogEF;

/// <summary>
/// 集成事件的发布状态枚举，用于 Outbox 模式。
/// 用于跟踪事件是否成功发布到消息总线（Message Bus）。
/// </summary>
public enum EventStateEnum
{
    /// <summary>
    /// 事件已存入数据库，但尚未尝试发布。
    /// </summary>
    NotPublished = 0,

    /// <summary>
    /// 事件正在发布中（已从数据库提取并处于发送队列或正在进行网络传输）。
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// 事件已成功发布并投递到消息队列。
    /// </summary>
    Published = 2,

    /// <summary>
    /// 事件发布失败（发生网络异常或 Broker 不可用）。
    /// </summary>
    PublishedFailed = 3
}


