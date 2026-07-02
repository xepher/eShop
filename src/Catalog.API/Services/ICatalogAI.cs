using Pgvector;

namespace eShop.Catalog.API.Services;

/// <summary>
/// 商品目录智能 AI 服务接口，定义商品向量化计算契约。
/// </summary>
public interface ICatalogAI
{
    /// <summary>
    /// 获取当前系统的 AI/向量引擎是否已成功配置并启用。
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 获取指定搜索文本的向量嵌入 (Embedding Vector)。
    /// </summary>
    ValueTask<Vector?> GetEmbeddingAsync(string text);
    
    /// <summary>
    /// 获取单个商品实体（基于名称、描述、品牌等属性串联）的向量嵌入。
    /// </summary>
    ValueTask<Vector?> GetEmbeddingAsync(CatalogItem item);

    /// <summary>
    /// 批量获取多个商品的向量嵌入列表，用于进行大规模商品数据播种或向量预加载。
    /// </summary>
    ValueTask<IReadOnlyList<Vector>?> GetEmbeddingsAsync(IEnumerable<CatalogItem> item);
}
