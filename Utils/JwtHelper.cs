using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApiDemo.Utils.Model;

namespace WebApiDemo.Utils
{
    /// <summary>
    /// JWT生成解析工具
    /// </summary>
    public class JwtHelper
    {
        private readonly IConfiguration _configuration;

        public JwtHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// 生成token
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public string CreateToken(TokenModelJwt model)
        {
            // 1. 定义需要使用到的Claims
            var claims = new List<Claim>
            {
                /*
                * 特别重要：
                1、这里将用户的部分信息，比如 uid 存到了Claim 中，如果你想知道如何在其他地方将这个 uid从 Token 中取出来，请看下边的SerializeJwt() 方法，或者在整个解决方案，搜索这个方法，看哪里使用了！
                2、你也可以研究下 HttpContext.User.Claims ，具体的你可以看看 Policys/PermissionHandler.cs 类中是如何使用的。
                */
                new Claim("UserId",model.UserId.ToString()),
                new Claim("UserName",model.UserName),
            };
            // 可以将一个用户的多个角色全部赋予；
            if (!string.IsNullOrWhiteSpace(model.Role))
            {
                claims.AddRange(model.Role.Split(',').Select(s => new Claim(ClaimTypes.Role, s)));
                claims.Add(new Claim("Role", model.Role));
            }
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var algorithm = SecurityAlgorithms.HmacSha256;
            var signingCredentials = new SigningCredentials(secretKey, algorithm);
            var jwtSecurityToken = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],     //Issuer
                _configuration["Jwt:Audience"],   //Audience
                claims,                           //Claims,
                DateTime.Now,                     //notBefore
                DateTime.Now.AddMinutes(5),      //expires 到期时间
                signingCredentials                //Credentials
            );
            var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            return token;
        }

        /// <summary>
        /// 解析
        /// </summary>
        /// <param name="jwtStr">如果为null将报错</param>
        /// <returns></returns>
        public static TokenModelJwt SerializeJwt(string jwtStr)
        {
            string str = "";
            try
            {
                if (jwtStr.Contains("Bearer "))
                {
                    str = jwtStr.Substring("Bearer ".Length, jwtStr.Length - "Bearer ".Length);
                }
                else
                {
                    str = jwtStr;
                }
                var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(str);
                var tokenJwt = JsonConvert.DeserializeObject<TokenModelJwt>(jwtToken.Payload.SerializeToJson());
                return tokenJwt;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
