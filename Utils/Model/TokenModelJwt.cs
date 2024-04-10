namespace WebApiDemo.Utils.Model
{
    /// <summary>
    /// 令牌
    /// </summary>
    public class TokenModelJwt
    {

        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string Secret { get; set; }
        public int Expires { get; set; }
        public string Role { get; set; }
    }
}
