using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace eShop.ServiceDefaults;

/// <summary>
/// .NET Aspire 服务默认设置扩展类。
/// 为应用程序主机 (AppHost) 所调用的每一个微服务项目提供统一的日志记录、可观测性 (OpenTelemetry)、服务发现、弹性重试机制以及健康检查终结点。
/// </summary>
public static partial class Extensions
{
    /// <summary>
    /// 为微服务注册完整的 Service Defaults 组件（包括遥测、服务发现、标准 HttpClient 弹性策略等）。
    /// </summary>
    /// <param name="builder">HostApplicationBuilder 实例</param>
    /// <returns>HostApplicationBuilder 实例支持链式调用</returns>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        // 1. 添加基础服务设置（包括健康检查和 OpenTelemetry）
        builder.AddBasicServiceDefaults();

        // 2. 启用 Microsoft Service Discovery（服务发现机制，解决微服务间的虚拟名称解析）
        builder.Services.AddServiceDiscovery();

        // 3. 配置全局 HttpClient 默认行为
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // 默认开启标准弹性重试管道（使用 Polly 提供的 Retry, Circuit Breaker, Timeout, Rate Limiter 策略）
            http.AddStandardResilienceHandler();

            // 默认配置 HttpClient 使用 Service Discovery 来解析微服务的逻辑终结点地址
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// 注册微服务的基础默认组件，但不配置传出的 HttpClient 重试。
    /// 这能允许不使用外呼 HTTP 调用的微服务避免引入 Polly，支持更轻量的修剪 (Trimming)。
    /// </summary>
    public static IHostApplicationBuilder AddBasicServiceDefaults(this IHostApplicationBuilder builder)
    {
        // 1. 注册默认的健康检查服务（用于监测自身健康状态）
        builder.AddDefaultHealthChecks();

        // 2. 注册并配置 OpenTelemetry 遥测指标、链路追踪和结构化日志
        builder.ConfigureOpenTelemetry();

        return builder;
    }

    /// <summary>
    /// 配置 OpenTelemetry，启用日志、指标与链路追踪收集，支持将信息上报给 Aspire Dashboard。
    /// </summary>
    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        // 1. 开启 OpenTelemetry 日志记录器提供程序
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true; // 记录格式化后的日志消息内容
            logging.IncludeScopes = true;           // 记录当前日志范围 (Scopes)，便于关联上下文（如 HTTP 请求追踪）
        });

        // 2. 启用并配置 Metrics（指标数据）与 Tracing（链路追踪）
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation() // 收集 ASP.NET Core 的 HTTP 请求指标
                    .AddHttpClientInstrumentation()   // 收集 HttpClient 发出的传出请求指标
                    .AddRuntimeInstrumentation()      // 收集 .NET 运行时指标（如 GC、线程池、内存）
                    .AddMeter("Experimental.Microsoft.Extensions.AI"); // 收集 Microsoft.Extensions.AI AI 组件库的实验性指标
            })
            .WithTracing(tracing =>
            {
                // 如果是本地开发环境，则开启全量采样，保证所有链路追踪信息都能呈现在 Dashboard 上
                if (builder.Environment.IsDevelopment())
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                }

                tracing.AddAspNetCoreInstrumentation() // 追踪入站 HTTP 请求链路
                    .AddGrpcClientInstrumentation()   // 追踪 gRPC 客户端传出请求链路
                    .AddHttpClientInstrumentation()   // 追踪 HttpClient 传出请求链路
                    .AddSource("Experimental.Microsoft.Extensions.AI"); // 追踪 AI 模型调用和向量嵌入操作链路
            });

        // 3. 配置并将收集到的指标数据和链路追踪通过 OTLP 协议导出
        builder.AddOpenTelemetryExporters();

        return builder;
    }

    /// <summary>
    /// 辅助方法：配置 OTLP (OpenTelemetry Protocol) 导出器。
    /// 只有在配置中提供了 "OTEL_EXPORTER_OTLP_ENDPOINT" 终结点时，才激活相应的日志、指标、追踪导出器。
    /// </summary>
    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            // 为 Logs、Metrics 和 Tracing 各自绑定 OTLP 导出器
            builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());
        }

        return builder;
    }

    /// <summary>
    /// 注册基本的健康检查组件。默认添加一个 "self" 节点（代表程序自身的存活健康状态 Liveness Check）。
    /// </summary>
    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // 添加默认的存活状态检查接口，始终返回 Healthy，带上 "live" 标签
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// 将默认的健康检查终结点映射到应用的路由表中（如 /health, /alive）。
    /// 出于安全考虑，默认仅在开发环境（Development）中直接开启健康检查终结点映射。
    /// 非开发环境启用请参考：https://aka.ms/dotnet/aspire/healthchecks
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // 提示：如需启用 Prometheus 拉取终结点，请引入 OpenTelemetry.Exporter.Prometheus.AspNetCore 并取消下行注释
        // app.MapPrometheusScrapingEndpoint();

        if (app.Environment.IsDevelopment())
        {
            // /health: 准备就绪检查 (Readiness Check)，所有注册的健康检查必须全部通过，表示微服务已就绪可接收流量
            app.MapHealthChecks("/health");

            // /alive: 存活检查 (Liveness Check)，仅校验打上了 "live" 标签的健康检查（如 self 检查）
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
