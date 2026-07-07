namespace eShop.IntegrationEventLogEF;

/// <summary>
/// 模型构建器扩展类，用于在主业务 DbContext 中注册和配置集成事件日志实体的数据库结构。
/// </summary>
public static class IntegrationLogExtensions
{
    /// <summary>
    /// 将 <see cref="IntegrationEventLogEntry"/> 实体映射并配置到数据库上下文中。
    /// 这使得主业务数据库中会生成一张名为 "IntegrationEventLog" 的表，用于存放待发布的 Outbox 事件。
    /// </summary>
    /// <param name="builder">EF Core 模型构建器实例。</param>
    public static void UseIntegrationEventLogs(this ModelBuilder builder)
    {
        builder.Entity<IntegrationEventLogEntry>(builder =>
        {
            // 将实体映射为特定的数据库表名
            builder.ToTable("IntegrationEventLog");

            // 指定 EventId 作为该表的主键
            builder.HasKey(e => e.EventId);
        });
    }
}

