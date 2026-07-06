namespace eShop.EventBus.Extensions;

/// <summary>
/// 泛型类型扩展方法类，用于获取更加友好的类型名称格式。
/// </summary>
public static class GenericTypeExtensions
{
    /// <summary>
    /// 获取泛型类型的友好字符串名称（例如，将 List`1 格式化为 List<Int32>）。
    /// </summary>
    /// <param name="type">要获取名称的 <see cref="Type"/>。</param>
    /// <returns>格式化后的类型名称字符串。</returns>
    public static string GetGenericTypeName(this Type type)
    {
        string typeName;

        if (type.IsGenericType)
        {
            var genericTypes = string.Join(",", type.GetGenericArguments().Select(t => t.Name).ToArray());
            typeName = $"{type.Name.Remove(type.Name.IndexOf('`'))}<{genericTypes}>";
        }
        else
        {
            typeName = type.Name;
        }

        return typeName;
    }

    /// <summary>
    /// 获取对象的泛型类型的友好字符串名称。
    /// </summary>
    /// <param name="object">要获取名称的对象实例。</param>
    /// <returns>格式化后的类型名称字符串。</returns>
    public static string GetGenericTypeName(this object @object)
    {
        return @object.GetType().GetGenericTypeName();
    }
}

