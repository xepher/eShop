using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Yarp;
using Aspire.Hosting.Yarp.Transforms;
using Yarp.ReverseProxy.Configuration;

namespace eShop.AppHost;

/// <summary>
/// 定义 OpenAI/Azure OpenAI 集成的部署目标类型
/// </summary>
internal enum OpenAITarget
{
    OpenAI,                     // 公共 OpenAI API
    AzureOpenAI,                // 托管于 Azure 并且使用 Aspire 自动配置资源的 OpenAI 实例
    AzureOpenAIExisting,        // 已有的 Azure OpenAI 实例（通过无密钥/管理身份连接）
    AzureOpenAIExistingWithKey  // 已有的 Azure OpenAI 实例（使用 API Key 连接）
}

internal static class Extensions
{
    /// <summary>
    /// 为分布式应用程序中的所有项目资源添加订阅器，自动将环境变量 ASPNETCORE_FORWARDEDHEADERS_ENABLED 设置为 true。
    /// 这能确保微服务在反向代理（如 YARP、Nginx 或 Kubernetes Ingress）后面运行时能正确识别 HTTPS/原始 Host 信息。
    /// </summary>
    public static IDistributedApplicationBuilder AddForwardedHeaders(this IDistributedApplicationBuilder builder)
    {
        builder.Services.TryAddEventingSubscriber<AddForwardHeadersSubscriber>();
        return builder;
    }

    /// <summary>
    /// 分布式应用事件订阅器，在应用启动前 (BeforeStartEvent) 遍历所有 Project 资源并注入环境变量。
    /// </summary>
    private class AddForwardHeadersSubscriber : IDistributedApplicationEventingSubscriber
    {
        public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            // 订阅启动前事件
            eventing.Subscribe<BeforeStartEvent>((@event, ct) =>
            {
                // 获取当前注册的所有项目资源 (ProjectResource)
                foreach (var p in @event.Model.GetProjectResources())
                {
                    // 动态添加环境回调注解，在运行时注入环境变量
                    p.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
                    {
                        context.EnvironmentVariables["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true";
                    }));
                }

                return Task.CompletedTask;
            });

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 配置 eShop 项目组件以集成 OpenAI/Azure OpenAI，用于文本向量嵌入 (Embedding) 和智能对话 (Chat)。
    /// </summary>
    /// <param name="builder">Aspire 应用程序生成器</param>
    /// <param name="catalogApi">目录 API 项目资源（用于计算商品向量嵌入）</param>
    /// <param name="webApp">电商 Web 前端（用于聊天交互）</param>
    /// <param name="openAITarget">集成类型目标</param>
    public static IDistributedApplicationBuilder AddOpenAI(this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> catalogApi,
        IResourceBuilder<ProjectResource> webApp,
        OpenAITarget openAITarget)
    {
        const string openAIName = "openai";

        // 向量模型相关常量
        const string textEmbeddingName = "textEmbeddingModel";
        const string textEmbeddingModelName = "text-embedding-3-small";

        // 聊天模型相关常量
        const string chatName = "chatModel";
        const string chatModelName = "gpt-4.1-mini";

        // 针对非自动化部署 Azure OpenAI 的情况（公共 OpenAI 或已有 Azure 实例），使用配置参数绑定
        if (openAITarget != OpenAITarget.AzureOpenAI)
        {
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            // 1. 如果不是公共 OpenAI，则需要 Azure 终结点参数
            IResourceBuilder<ParameterResource>? endpoint = null;
            if (openAITarget != OpenAITarget.OpenAI)
            {
                endpoint = builder.AddParameter("OpenAIEndpointParameter")
                    .WithDescription("The Azure OpenAI endpoint to use, e.g. https://<name>.openai.azure.com/")
                    .WithCustomInput(p => new()
                    {
                        Name = "OpenAIEndpointParameter",
                        Label = "Azure OpenAI Endpoint",
                        InputType = InputType.Text,
                        Value = "https://<name>.openai.azure.com/",
                    });
            }

            // 2. 如果是公共 OpenAI 或者显式指定 API 密钥的 Azure 实例，需要提供 API 密钥参数
            IResourceBuilder<ParameterResource>? key = null;
            if (openAITarget is OpenAITarget.OpenAI or OpenAITarget.AzureOpenAIExistingWithKey)
            {
                key = builder.AddParameter("OpenAIKeyParameter", secret: true)
                    .WithDescription("The OpenAI API key to use.")
                    .WithCustomInput(p => new()
                    {
                        Name = "OpenAIKeyParameter",
                        Label = "API Key",
                        InputType = InputType.SecretText
                    });
            }

            // 3. 聊天模型名称参数（例如 gpt-4.1-mini）
            var chatModel = builder.AddParameter("ChatModelParameter")
                .WithDescription("The chat model to use.")
                .WithCustomInput(p => new()
                {
                    Name = "ChatModelParameter",
                    Label = "Chat Model",
                    InputType = InputType.Text,
                    Value = chatModelName,
                });

            // 4. 文本向量嵌入模型名称参数（例如 text-embedding-3-small）
            var embeddingModel = builder.AddParameter("EmbeddingModelParameter")
                .WithDescription("The embedding model to use.")
                .WithCustomInput(p => new()
                {
                    Name = "EmbeddingModelParameter",
                    Label = "Text Embedding Model",
                    InputType = InputType.Text,
                    Value = textEmbeddingModelName,
                });
#pragma warning restore ASPIREINTERACTION001

            // 构建统一的连接字符串表达式
            var openAIConnectionBuilder = new ReferenceExpressionBuilder();
            if (endpoint is not null)
            {
                openAIConnectionBuilder.Append($"Endpoint={endpoint}");
            }
            if (key is not null)
            {
                openAIConnectionBuilder.Append($";Key={key}");
            }
            var openAIConnectionString = openAIConnectionBuilder.Build();

            // 为 catalogApi 注入 Embedding 模型连接字符串
            catalogApi.WithReference(builder.AddConnectionString(textEmbeddingName, cs =>
            {
                cs.Append($"{openAIConnectionString};Deployment={embeddingModel}");
            }));
            // 为 webApp 注入 Chat 模型连接字符串
            webApp.WithReference(builder.AddConnectionString(chatName, cs =>
            {
                cs.Append($"{openAIConnectionString};Deployment={chatModel}");
            }));
        }
        else
        {
            // 使用 Azure OpenAI 资源提供者，自动集成并部署 Azure OpenAI 服务及其子部署 (Deployment)
            var openAI = builder.AddAzureOpenAI(openAIName);

            // 添加 Chat 部署
            var chat = openAI.AddDeployment(chatName, chatModelName, "2025-04-14")
                .WithProperties(d =>
                {
                    d.DeploymentName = chatModelName;
                    d.SkuName = "GlobalStandard";
                    d.SkuCapacity = 50;
                });
            // 添加 向量嵌入 部署
            var textEmbedding = openAI.AddDeployment(textEmbeddingName, textEmbeddingModelName, "1")
                .WithProperties(d =>
                {
                    d.DeploymentName = textEmbeddingModelName;
                    d.SkuCapacity = 20; // 需要至少 20k tokens/min 的吞吐量以用于数据库初次播种时的向量计算
                });

            // 注入引用
            catalogApi.WithReference(textEmbedding);
            webApp.WithReference(chat);
        }

        return builder;
    }

    /// <summary>
    /// 配置 eShop 项目以使用本地 Ollama 容器来运行开源大模型（如 llama3.1、all-minilm）。
    /// </summary>
    public static IDistributedApplicationBuilder AddOllama(this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> catalogApi,
        IResourceBuilder<ProjectResource> webApp)
    {
        // 启动 Ollama 容器，配置数据卷挂载以防重复拉取模型，启用 GPU 加速，同时启用 Web 控制台 (Open-WebUI)
        var ollama = builder.AddOllama("ollama")
            .WithDataVolume()
            .WithGPUSupport()
            .WithOpenWebUI();
        
        // 声明需要拉取的模型：all-minilm 向量模型与 llama3.1 聊天模型
        var embeddings = ollama.AddModel("embedding", "all-minilm");
        var chat = ollama.AddModel("chat", "llama3.1");

        // 将模型终结点注入微服务，并等待模型拉取完毕
        catalogApi.WithReference(embeddings)
            .WithEnvironment("OllamaEnabled", "true")
            .WaitFor(embeddings);
        webApp.WithReference(chat)
            .WithEnvironment("OllamaEnabled", "true")
            .WaitFor(chat);

        return builder;
    }

    /// <summary>
    /// 配置移动端 BFF (mobile-bff) 的反向代理路由规则 (使用 YARP)。
    /// 将特定的外部 API 请求精确转发到后台各微服务中。
    /// </summary>
    public static IResourceBuilder<YarpResource> ConfigureMobileBffRoutes(this IResourceBuilder<YarpResource> builder,
        IResourceBuilder<ProjectResource> catalogApi,
        IResourceBuilder<ProjectResource> orderingApi,
        IResourceBuilder<ProjectResource> identityApi)
    {
        return builder.WithConfiguration(yarp =>
        {
            // 为 Catalog.API 创建一个 YARP 后端集群 (Cluster)
            var catalogCluster = yarp.AddCluster(catalogApi);

            // ==========================================
            // 商品目录 (Catalog) 路由映射与版本匹配
            // ==========================================

            // 1. 获取商品列表
            yarp.AddRoute("/catalog-api/api/catalog/items", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1", "2.0"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            // 2. 根据多个 ID 批量获取商品
            yarp.AddRoute("/catalog-api/api/catalog/items/by", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1", "2.0"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            // 3. 根据单个 ID 获取商品详情
            yarp.AddRoute("/catalog-api/api/catalog/items/{id}", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1", "2.0"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            // 4. 根据名称搜索商品 (限 v1)
            yarp.AddRoute("/catalog-api/api/catalog/items/by/{name}", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            // 5. 根据文本进行语义向量检索 (v1 路径参数形式)
            yarp.AddRoute("/catalog-api/api/catalog/items/withsemanticrelevance/{text}", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            // 6. 根据文本进行语义向量检索 (v2 查询参数形式)
            yarp.AddRoute("/catalog-api/api/catalog/items/withsemanticrelevance", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["2.0"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            // 7. 按类型和品牌获取商品列表
            yarp.AddRoute("/catalog-api/api/catalog/items/type/{typeId}/brand/{brandId?}", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            // 8. 获取指定品牌的全部类型商品列表
            yarp.AddRoute("/catalog-api/api/catalog/items/type/all/brand/{brandId?}", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            // 9. 获取所有的商品类型
            yarp.AddRoute("/catalog-api/api/catalog/catalogTypes", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1", "2.0"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            // 10. 获取所有的商品品牌
            yarp.AddRoute("/catalog-api/api/catalog/catalogBrands", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1", "2.0"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            // 11. 获取商品图片
            yarp.AddRoute("/catalog-api/api/catalog/items/{id}/pic", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1", "2.0"], Mode = QueryParameterMatchMode.Exact }])
                .WithTransformPathRemovePrefix("/catalog-api");

            // 12. 通用 Catalog 兜底捕获路由
            yarp.AddRoute("/api/catalog/{*any}", catalogCluster)
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1", "2.0"], Mode = QueryParameterMatchMode.Exact }]);

            // ==========================================
            // 订单 (Ordering) 路由映射
            // ==========================================
            yarp.AddRoute("/api/orders/{*any}", orderingApi.GetEndpoint("http"))
                .WithMatchRouteQueryParameter([new() { Name = "api-version", Values = ["1.0", "1"], Mode = QueryParameterMatchMode.Exact }]);

            // ==========================================
            // 身份验证 (Identity) 路由映射
            // ==========================================
            yarp.AddRoute("/identity/{*any}", identityApi.GetEndpoint("http"))
                .WithTransformPathRemovePrefix("/identity");
        });
    }
}
