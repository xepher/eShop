using System.ComponentModel.DataAnnotations;

namespace eShop.IntegrationEventLogEF;

/// <summary>
/// 表示在数据库中记录的集成事件日志条目，用于 Outbox 模式的持久化存储。
/// 它将强类型的集成事件对象序列化为 JSON 字符串存储在数据库中，以确保本地事务与事件发布的最终一致性。
/// </summary>
public class IntegrationEventLogEntry
{
    // 用于序列化事件内容时，格式化输出（带有缩进和美化）的 JSON 选项
    private static readonly JsonSerializerOptions s_indentedOptions = new() { WriteIndented = true };
    // 用于反序列化事件内容时，忽略属性大小写敏感性的 JSON 选项
    private static readonly JsonSerializerOptions s_caseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// 无参构造函数，专供 EF Core 反序列化及映射使用。
    /// </summary>
    private IntegrationEventLogEntry() { }

    /// <summary>
    /// 初始化一个新的集成事件日志条目。
    /// </summary>
    /// <param name="event">要持久化记录的集成事件实例。</param>
    /// <param name="transactionId">与该事件绑定在同一个本地事务中的事务唯一标识符。</param>
    public IntegrationEventLogEntry(IntegrationEvent @event, Guid transactionId)
    {
        EventId = @event.Id;
        CreationTime = @event.CreationDate;
        EventTypeName = @event.GetType().FullName;
        // 将具体的集成事件子类对象序列化为 JSON 文本存储到数据库
        Content = JsonSerializer.Serialize(@event, @event.GetType(), s_indentedOptions);
        State = EventStateEnum.NotPublished;
        TimesSent = 0;
        TransactionId = transactionId;
    }

    /// <summary>
    /// 获取集成事件的唯一标识符（主键）。
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// 获取集成事件类型的完整类名（包含命名空间，如 "eShop.Catalog.API.IntegrationEvents.Events.ProductPriceChangedIntegrationEvent"）。
    /// 用于反序列化时动态还原类型。
    /// </summary>
    [Required]
    public string EventTypeName { get; private set; }

    /// <summary>
    /// 获取集成事件的简称（不带命名空间，如 "ProductPriceChangedIntegrationEvent"）。
    /// 该属性不映射到数据库。
    /// </summary>
    [NotMapped]
    public string EventTypeShortName => EventTypeName.Split('.')?.Last();

    /// <summary>
    /// 获取反序列化还原后的强类型 <see cref="IntegrationEvent"/> 实例。
    /// 该属性不映射到数据库。
    /// </summary>
    [NotMapped]
    public IntegrationEvent IntegrationEvent { get; private set; }

    /// <summary>
    /// 获取或设置集成事件的发布状态。
    /// </summary>
    public EventStateEnum State { get; set; }

    /// <summary>
    /// 获取或设置该消息已尝试发送的次数。
    /// </summary>
    public int TimesSent { get; set; }

    /// <summary>
    /// 获取事件创建时间。
    /// </summary>
    public DateTime CreationTime { get; private set; }

    /// <summary>
    /// 获取存储在数据库中的集成事件的 JSON 序列化内容。
    /// </summary>
    [Required]
    public string Content { get; private set; }

    /// <summary>
    /// 获取与此条目关联的本地数据库事务标识。
    /// 用来确保在事务提交后，仅提取特定事务产生的事件进行发布。
    /// </summary>
    public Guid TransactionId { get; private set; }

    /// <summary>
    /// 将存储的 JSON 文本内容反序列化为具体的 C# 集成事件子类对象。
    /// </summary>
    /// <param name="type">要还原的具体集成事件的目标运行时类型（System.Type）。</param>
    /// <returns>返回当前条目实例，支持链式调用。</returns>
    public IntegrationEventLogEntry DeserializeJsonContent(Type type)
    {
        IntegrationEvent = JsonSerializer.Deserialize(Content, type, s_caseInsensitiveOptions) as IntegrationEvent;
        return this;
    }
}

