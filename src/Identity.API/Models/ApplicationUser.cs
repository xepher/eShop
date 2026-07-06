namespace eShop.Identity.API.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    /// <summary>
    /// 应用程序用户模型，继承自 ASP.NET Core Identity 的 <see cref="IdentityUser"/>。
    /// 添加了微服务中下单和送货所需的额外个人资料字段（信用卡信息和地址信息）。
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// 信用卡卡号。
        /// </summary>
        [Required]
        public string CardNumber { get; set; }

        /// <summary>
        /// 信用卡安全码（CVV）。
        /// </summary>
        [Required]
        public string SecurityNumber { get; set; }

        /// <summary>
        /// 信用卡过期时间，格式应为 MM/YY（例如 12/24）。
        /// </summary>
        [Required]
        [RegularExpression(@"(0[1-9]|1[0-2])\/[0-9]{2}", ErrorMessage = "Expiration should match a valid MM/YY value")]
        public string Expiration { get; set; }

        /// <summary>
        /// 持卡人姓名。
        /// </summary>
        [Required]
        public string CardHolderName { get; set; }

        /// <summary>
        /// 信用卡类型标识。
        /// </summary>
        public int CardType { get; set; }

        /// <summary>
        /// 街道地址。
        /// </summary>
        [Required]
        public string Street { get; set; }

        /// <summary>
        /// 城市。
        /// </summary>
        [Required]
        public string City { get; set; }

        /// <summary>
        /// 州 / 省。
        /// </summary>
        [Required]
        public string State { get; set; }

        /// <summary>
        /// 国家。
        /// </summary>
        [Required]
        public string Country { get; set; }

        /// <summary>
        /// 邮政编码。
        /// </summary>
        [Required]
        public string ZipCode { get; set; }

        /// <summary>
        /// 名字（First Name）。
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// 姓氏（Last Name）。
        /// </summary>
        [Required]
        public string LastName { get; set; }
    }
}

