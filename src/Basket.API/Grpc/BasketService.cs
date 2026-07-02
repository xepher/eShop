using System.Diagnostics.CodeAnalysis;
using eShop.Basket.API.Repositories;
using eShop.Basket.API.Extensions;
using eShop.Basket.API.Model;

namespace eShop.Basket.API.Grpc;

/// <summary>
/// 购物车 gRPC 服务实现类，为前端 BFF 网关或其他后端服务提供高性能 RPC 接口。
/// 继承自根据 Proto 协议文件自动生成的 Basket.BasketBase 基类。
/// </summary>
public class BasketService(
    IBasketRepository repository,
    ILogger<BasketService> logger) : Basket.BasketBase
{
    /// <summary>
    /// 获取用户购物车详情。该接口允许匿名访问（在方法层面添加 [AllowAnonymous]），
    /// 如果未携带身份信息则返回空购物车。
    /// </summary>
    [AllowAnonymous]
    public override async Task<CustomerBasketResponse> GetBasket(GetBasketRequest request, ServerCallContext context)
    {
        // 从 gRPC 上下文中读取用户唯一 ID (OIDC Subject)
        var userId = context.GetUserIdentity();
        if (string.IsNullOrEmpty(userId))
        {
            return new(); // 未登录用户，返回空响应
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Begin GetBasketById call from method {Method} for basket id {Id}", context.Method, userId);
        }

        // 从仓储中异步获取购物车实体
        var data = await repository.GetBasketAsync(userId);

        if (data is not null)
        {
            // 将内部的 CustomerBasket 实体映射为 protobuf 定义的 CustomerBasketResponse 响应对象
            return MapToCustomerBasketResponse(data);
        }

        return new();
    }

    /// <summary>
    /// 更新或创建用户购物车内容。必须是经过授权登录的用户方能调用。
    /// </summary>
    public override async Task<CustomerBasketResponse> UpdateBasket(UpdateBasketRequest request, ServerCallContext context)
    {
        var userId = context.GetUserIdentity();
        if (string.IsNullOrEmpty(userId))
        {
            ThrowNotAuthenticated(); // 未登录，直接抛出 gRPC 未授权异常 (Unauthenticated)
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Begin UpdateBasket call from method {Method} for basket id {Id}", context.Method, userId);
        }

        // 将请求参数转换为 CustomerBasket 模型
        var customerBasket = MapToCustomerBasket(userId, request);
        
        // 保存并持久化至 Redis
        var response = await repository.UpdateBasketAsync(customerBasket);
        if (response is null)
        {
            ThrowBasketDoesNotExist(userId); // 异常处理：保存失败，抛出 NotFound 异常
        }

        return MapToCustomerBasketResponse(response);
    }

    /// <summary>
    /// 删除指定用户的购物车缓存。通常在用户下单完成或清空购物车时调用。
    /// </summary>
    public override async Task<DeleteBasketResponse> DeleteBasket(DeleteBasketRequest request, ServerCallContext context)
    {
        var userId = context.GetUserIdentity();
        if (string.IsNullOrEmpty(userId))
        {
            ThrowNotAuthenticated();
        }

        await repository.DeleteBasketAsync(userId);
        return new();
    }

    /// <summary>
    /// 辅助方法：向调用方抛出 gRPC 标准未授权状态异常
    /// </summary>
    [DoesNotReturn]
    private static void ThrowNotAuthenticated() => throw new RpcException(new Status(StatusCode.Unauthenticated, "The caller is not authenticated."));

    /// <summary>
    /// 辅助方法：向调用方抛出 gRPC 标准资源未找到状态异常
    /// </summary>
    [DoesNotReturn]
    private static void ThrowBasketDoesNotExist(string userId) => throw new RpcException(new Status(StatusCode.NotFound, $"Basket with buyer id {userId} does not exist"));

    /// <summary>
    /// 映射方法：将业务实体映射为 Protobuf Message 对象
    /// </summary>
    private static CustomerBasketResponse MapToCustomerBasketResponse(CustomerBasket customerBasket)
    {
        var response = new CustomerBasketResponse();

        foreach (var item in customerBasket.Items)
        {
            response.Items.Add(new BasketItem()
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
            });
        }

        return response;
    }

    /// <summary>
    /// 映射方法：将 Protobuf Request 对象转换为业务实体模型
    /// </summary>
    private static CustomerBasket MapToCustomerBasket(string userId, UpdateBasketRequest customerBasketRequest)
    {
        var response = new CustomerBasket
        {
            BuyerId = userId
        };

        foreach (var item in customerBasketRequest.Items)
        {
            response.Items.Add(new()
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
            });
        }

        return response;
    }
}
