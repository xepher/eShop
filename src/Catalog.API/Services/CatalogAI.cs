using System.Diagnostics;
using Microsoft.Extensions.AI;
using Pgvector;

namespace eShop.Catalog.API.Services;

/// <summary>
/// 商品目录智能 AI 服务实现类。
/// 封装 Microsoft.Extensions.AI 的 IEmbeddingGenerator 接口，执行向量的计算与裁剪。
/// </summary>
public sealed class CatalogAI : ICatalogAI
{
    // 定义向量的维度（384 维对应 "all-minilm" 或被裁剪后的向量特征维度，适合 pgvector 快速检索）
    private const int EmbeddingDimensions = 384;
    private readonly IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;

    /// <summary>Web 宿主环境实例</summary>
    private readonly IWebHostEnvironment _environment;
    /// <summary>日志记录器</summary>
    private readonly ILogger _logger;

    public CatalogAI(IWebHostEnvironment environment, ILogger<CatalogAI> logger, IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null)
    {
        _embeddingGenerator = embeddingGenerator;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// 判断是否启用了 AI 向量计算服务（即已成功注册了 IEmbeddingGenerator 生成器）
    /// </summary>
    public bool IsEnabled => _embeddingGenerator is not null;

    /// <summary>
    /// 异步获取单个商品实体的向量嵌入
    /// </summary>
    public ValueTask<Vector?> GetEmbeddingAsync(CatalogItem item) =>
        IsEnabled ?
            GetEmbeddingAsync(CatalogItemToString(item)) :
            ValueTask.FromResult<Vector?>(null);

    /// <summary>
    /// 异步批量获取多个商品的向量嵌入列表
    /// </summary>
    public async ValueTask<IReadOnlyList<Vector>?> GetEmbeddingsAsync(IEnumerable<CatalogItem> items)
    {
        if (IsEnabled)
        {
            long timestamp = Stopwatch.GetTimestamp();

            // 调用 AI Generator 生成指定一批商品字符串描述的向量数据
            GeneratedEmbeddings<Embedding<float>> embeddings = await _embeddingGenerator!.GenerateAsync(items.Select(CatalogItemToString));
            
            // 提取并裁剪向量到预设的 384 维度以支持数据库的高效存储
            var results = embeddings.Select(m => new Vector(m.Vector[0..EmbeddingDimensions])).ToList();

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Generated {EmbeddingsCount} embeddings in {ElapsedMilliseconds}s", results.Count, Stopwatch.GetElapsedTime(timestamp).TotalSeconds);
            }

            return results;
        }

        return null;
    }

    /// <summary>
    /// 异步获取纯文本的向量嵌入
    /// </summary>
    public async ValueTask<Vector?> GetEmbeddingAsync(string text)
    {
        if (IsEnabled)
        {
            long timestamp = Stopwatch.GetTimestamp();

            // 生成纯文本的向量表示
            var embedding = await _embeddingGenerator!.GenerateVectorAsync(text);
            
            // 裁剪向量维度
            embedding = embedding[0..EmbeddingDimensions];

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Generated embedding in {ElapsedMilliseconds}s: '{Text}'", Stopwatch.GetElapsedTime(timestamp).TotalSeconds, text);
            }

            return new Vector(embedding);
        }

        return null;
    }

    /// <summary>
    /// 辅助方法：将商品实体转换为用于计算向量嵌入的纯文本代表字段（串联商品名称与描述）
    /// </summary>
    private static string CatalogItemToString(CatalogItem item) => $"{item.Name} {item.Description}";
}
