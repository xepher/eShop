using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace eShop.EventBus.Abstractions;

/// <summary>
/// 事件总线订阅信息类。
/// 保存了集成事件的类型映射，以及反序列化集成事件时使用的 JSON 序列化选项。
/// </summary>
public class EventBusSubscriptionInfo
{
    /// <summary>
    /// 事件类型映射字典。
    /// 键 (Key) 为集成事件的唯一标识名称（通常为类型名），值 (Value) 为集成事件的运行时 <see cref="Type"/>。
    /// </summary>
    public Dictionary<string, Type> EventTypes { get; } = [];

    /// <summary>
    /// 获取当前订阅所使用的 JSON 序列化配置选项。
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; } = new(DefaultSerializerOptions);

    /// <summary>
    /// 默认的 JSON 序列化配置。
    /// 为了兼容 Native AOT 编译，在未启用反射时，使用空的合并解析器；启用反射时使用默认解析器。
    /// </summary>
    internal static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault ? CreateDefaultTypeResolver() : JsonTypeInfoResolver.Combine()
    };

#pragma warning disable IL2026
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
    /// <summary>
    /// 创建默认的基于反射的 JSON 类型信息解析器。
    /// </summary>
    private static IJsonTypeInfoResolver CreateDefaultTypeResolver()
        => new DefaultJsonTypeInfoResolver();
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning restore IL2026
}

