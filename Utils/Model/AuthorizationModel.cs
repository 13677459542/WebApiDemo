namespace WebApiDemo.Utils.Model
{
    /// <summary>
    /// 授权
    /// </summary>
    public class AuthorizationModel
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string Pwd { get; set; }

        /// <summary>
        /// 随机字符串
        /// </summary>
        public string nonce_str { get; set; }
    }
}
