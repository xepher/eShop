using System.ComponentModel.DataAnnotations;

namespace eShop.Ordering.Infrastructure.Idempotency;

/// <summary>
/// 客户端请求实体，用于记录已处理命令的幂等性信息，防止同一个请求重复执行。
/// </summary>
public class ClientRequest
{
    /// <summary>
    /// 请求的唯一标识 (Guid)，通常是客户端发送 Command 时携带的 RequestId。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 命令或请求的类型名称。
    /// </summary>
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// 该请求被处理并记录的时间（UTC 时间）。
    /// </summary>
    public DateTime Time { get; set; }
}
