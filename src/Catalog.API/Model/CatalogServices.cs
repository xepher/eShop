using eShop.Catalog.API.Services;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// 服务聚合类（CatalogServices）。
/// 作为一个 Parameter Object，通过 [AsParameters] 特性在 Minimal API 路由处理方法中整体注入，
/// 避免方法签名中罗列过多服务参数，提高路由端点代码的可读性与未来的可维护性。
/// </summary>
public class CatalogServices(
    CatalogContext context,
    [FromServices] ICatalogAI catalogAI,
    IOptions<CatalogOptions> options,
    ILogger<CatalogServices> logger,
    [FromServices] ICatalogIntegrationEventService eventService)
{
    public CatalogContext Context { get; } = context;
    public ICatalogAI CatalogAI { get; } = catalogAI;
    public IOptions<CatalogOptions> Options { get; } = options;
    public ILogger<CatalogServices> Logger { get; } = logger;
    public ICatalogIntegrationEventService EventService { get; } = eventService;
};
