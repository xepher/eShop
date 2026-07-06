namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 事件总线构建器接口，用于在依赖注入容器中配置事件总线及其相关的集成事件订阅。
/// </summary>
public interface IEventBusBuilder
{
    /// <summary>
    /// 获取当前的依赖注入服务集合。
    /// </summary>
    public IServiceCollection Services { get; }
}

