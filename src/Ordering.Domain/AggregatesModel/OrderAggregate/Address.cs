using eShop.Ordering.Domain.SeedWork;

namespace eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

/// <summary>
/// 地址值对象。表示订单的配送地址，具备不可变性和基于属性值的相等性比较。
/// </summary>
public class Address : ValueObject
{
    /// <summary>
    /// 街道地址。
    /// </summary>
    public string Street { get; private set; }

    /// <summary>
    /// 城市。
    /// </summary>
    public string City { get; private set; }

    /// <summary>
    /// 省份/州。
    /// </summary>
    public string State { get; private set; }

    /// <summary>
    /// 国家。
    /// </summary>
    public string Country { get; private set; }

    /// <summary>
    /// 邮政编码。
    /// </summary>
    public string ZipCode { get; private set; }

    /// <summary>
    /// 用于 ORM (EF Core) 反序列化或映射的无参构造函数。
    /// </summary>
    public Address() { }

    /// <summary>
    /// 创建地址值对象的构造函数。
    /// </summary>
    /// <param name="street">街道地址。</param>
    /// <param name="city">城市。</param>
    /// <param name="state">省份/州。</param>
    /// <param name="country">国家。</param>
    /// <param name="zipcode">邮政编码。</param>
    public Address(string street, string city, string state, string country, string zipcode)
    {
        Street = street;
        City = city;
        State = state;
        Country = country;
        ZipCode = zipcode;
    }

    /// <summary>
    /// 获取用于计算相等性的属性组件集合，这是实现值对象相等性比较的基础。
    /// </summary>
    /// <returns>包含各个地址属性的迭代器。</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        // 使用 yield return 逐个返回各个属性组件，用于在基类中进行值相等性比较
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return ZipCode;
    }
}
