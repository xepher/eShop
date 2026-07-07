namespace eShop.Ordering.Infrastructure;

/// <summary>
/// 介质器扩展类，提供基于 EF Core 实体追踪的状态分发领域事件的方法。
/// </summary>
static class MediatorExtension
{
    /// <summary>
    /// 异步分发所有的领域事件。
    /// 从当前 OrderingContext 追踪的所有 Entity 中，收集所有挂起的领域事件，清除实体内的事件列表，并依次通过 MediatR 进行发布。
    /// </summary>
    /// <param name="mediator">MediatR 实例。</param>
    /// <param name="ctx">订单数据库上下文。</param>
    /// <returns>异步任务。</returns>
    public static async Task DispatchDomainEventsAsync(this IMediator mediator, OrderingContext ctx)
    {
        // 1. 获取所有已被 EF Core ChangeTracker 追踪，且继承自 Entity 类，并且包含未分发领域事件的实体实体实体
        var domainEntities = ctx.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any());

        // 2. 提取所有这些实体中的领域事件列表
        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        // 3. 清空各个实体内部的领域事件，防止事件重复分发或陷入死循环
        domainEntities.ToList()
            .ForEach(entity => entity.Entity.ClearDomainEvents());

        // 4. 循环发布每一个领域事件，触发订阅了这些事件的 Domain Event Handlers
        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent);
    }
}
