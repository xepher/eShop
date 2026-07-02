using System.Reflection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// 宿主环境 (IHostEnvironment) 的内部扩展方法类。
/// </summary>
internal static class HostEnvironmentExtensions
{
    /// <summary>
    /// 判断当前应用是否在编译/生成 OpenAPI 文档的构建阶段运行。
    /// 可以检查当前环境名称是否为 "Build"，或者当前进程入口程序集是否为 .NET OpenAPI 生成工具 "GetDocument.Insider"。
    /// </summary>
    public static bool IsBuild(this IHostEnvironment hostEnvironment)
    {
        return hostEnvironment.IsEnvironment("Build") || Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
    }
}
