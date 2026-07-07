namespace eShop.Ordering.Infrastructure.EntityConfigurations;

/// <summary>
/// 幂等性客户端请求 (ClientRequest) 实体的 EF Core 映射配置类。
/// </summary>
class ClientRequestEntityTypeConfiguration
    : IEntityTypeConfiguration<ClientRequest>
{
    /// <summary>
    /// 配置幂等请求记录表的数据库映射。
    /// </summary>
    /// <param name="requestConfiguration">客户端请求实体构建器。</param>
    public void Configure(EntityTypeBuilder<ClientRequest> requestConfiguration)
    {
        // 映射到名为 "requests" 的数据库表，用于记录幂等操作的请求标识
        requestConfiguration.ToTable("requests");
    }
}
