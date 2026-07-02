namespace Microsoft.Extensions.Configuration;

/// <summary>
/// IConfiguration 接口扩展方法类，用于确保关键配置字段存在，并在缺失时抛出友好异常。
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// 获取指定的配置值。如果该配置项为空，则抛出 InvalidOperationException 异常并输出具体的配置键路径，方便快速定位排错。
    /// </summary>
    /// <param name="configuration">配置实例或配置节</param>
    /// <param name="name">配置键名</param>
    /// <returns>配置的值</returns>
    /// <exception cref="InvalidOperationException">当配置值不存在或为空时抛出</exception>
    public static string GetRequiredValue(this IConfiguration configuration, string name) =>
        configuration[name] ?? throw new InvalidOperationException($"Configuration missing value for: {(configuration is IConfigurationSection s ? s.Path + ":" + name : name)}");
}
