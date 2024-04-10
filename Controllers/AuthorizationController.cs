using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using WebApiDemo.Model.Resp;
using WebApiDemo.Utils;
using WebApiDemo.Utils.Model;

namespace WebApiDemo.Controllers
{
    /// <summary>
    /// 授权
    /// </summary>
    [Route("Api/V1/[controller]/[action]")]
    // [Authorize]
    [ApiController]
    public class AuthorizationController : Controller
    {
        private readonly JwtHelper _jwtHelper;
        //private readonly IAuthorizatoinsService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthorizationController> _logger;

        /// <summary>
        /// 2048 公钥
        /// </summary>
        private readonly string publicKey = @"MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA2n9uC3TN82DAzRL8rpDR3Jpy6d4IGUDebLwxsRWaZU8gIpWbzy/eHbVpQyMdAGSZxyfT6/UFmHfIdVIiLBmYVcY3rZ0G3+rNePzEzPzLaFOOFbhWTP+L2uL/FRwx/hpHpsPawd76wTmxxYKelx5ELciqD1ISIc6gpQWcARMfwqB1uPNP/JJithju+2lN8rCDk+MLhgSe8UzGM+V2eP/lfIPDSBKZ40FBKXur3uC8HyNqbwCjovM7yRJXyV/MACWyDQjdYN1f9WMNqQp2kXg0HKPDhEUyHkcZ0RpxxeybexJ9VZqbTKIl8ENkTcXxevmYsAsVmC8cr/Hp2OnowQR+EwIDAQAB";
        /// <summary>
        ///  //2048 私钥
        /// </summary>
        private readonly string privateKey = @"MIIEpAIBAAKCAQEA2n9uC3TN82DAzRL8rpDR3Jpy6d4IGUDebLwxsRWaZU8gIpWbzy/eHbVpQyMdAGSZxyfT6/UFmHfIdVIiLBmYVcY3rZ0G3+rNePzEzPzLaFOOFbhWTP+L2uL/FRwx/hpHpsPawd76wTmxxYKelx5ELciqD1ISIc6gpQWcARMfwqB1uPNP/JJithju+2lN8rCDk+MLhgSe8UzGM+V2eP/lfIPDSBKZ40FBKXur3uC8HyNqbwCjovM7yRJXyV/MACWyDQjdYN1f9WMNqQp2kXg0HKPDhEUyHkcZ0RpxxeybexJ9VZqbTKIl8ENkTcXxevmYsAsVmC8cr/Hp2OnowQR+EwIDAQABAoIBAQDRgmkJkSm+GeMlgPRLis/AgVR4zY7kcCAXEWlwjO9r/zAoGV66jwKjaAUT/EJd0xjlL1p0oZCI/yp23Jepw60fah6PWcdyxBnbzjwC9s8wLRZL22LdGBiJfSnsmwmQxrA3xwsm5OF6kBDW+4WID9x+LjBq2l5Kjm/ZbISHP0gv/Ji/jga2kT8HoYvcUmuer9pLBk6E3yrq0LBvR3FsT+qkOHFEfMpuSB61GT15THZjxjEbl6pFqhEpNsMSvFj7AamOD+F2I0WZjamsd2DSWltKtyqvlWrhbokDIgpsJ9dVW62kAWtFmiFT5UhsrQOU81rhmVwUGTPt0LICy5HTWOlBAoGBAPPOC9aROf1cSNRowEiajfpH0BkAnjTmRKwFc9UdmRg6fBY9SdJpr2VqKwWsFD3rvaVRiL1TbhrmBOSC7tYszrRYrOc12DLh4qYMjd3NNZNuABvJyA+BMsK4cBx/UprXGEN/XvHab+SPGUZ1aOOHDwhPChUqqGkIhwHDr2qvVwgzAoGBAOVtUqmOgqFex9Lqbr/2oZwPvCMYljZx2vSodDGkl/sGtiAKfJZ2iFV9vFMqIldDPwhvYn0LAHmSY1g6M7Edp7Cw4vfoUDIYhPdoJIAOZOGjYa7QU/B3T09zg63LRFp+tNl9RAMStaF73I4BJO3X7PXFcRrmw70vwIWOhMZyuFKhAoGAcFneeLWaDJiPc1sGaS7YCKM5UZxIS8ZllQQ6OdaW62RgNHtv3ogXbNu9EbMX7OULEvj804pz7e9cB9YSrB4f71oB69aTV/diU/TrF3BupQ8G+8dD62k1dCg8edVuwq4mn0w9+6QW9jO/iQmoGVnu4nxSACkVTLnCRVzhJH/C0qMCgYEAs9/QbWtz83zSAgUXK537+tVDVejS7IC7gBIKd1lqZr9OTzSplXX9Ubmwyys/nVb1tnFNsGfNyYMCLIwFNxne/WLRsDgNmBktNqQJ6fRfF6D21w4yoVeJcOtKFBpHzwOEWvghOJ+Uk7T+qL8w6uDdwZs5IDRIxq0Hri6c3tHWvcECgYBqZ2L07yKesEUtpq6aLVIw3DPUO+hGqW+B6ydzG+KIbrrzN1sxjNTGFkfwSM+2X8bCZcOHJdZop/7LtNaIPzD9fCH1G9MEkG8KcZycwmV2kWt1xxZo7D6OiwvcNOTBHB3ZH5HjrZqq4M3fFi8YuJ2vWGdq11X9s0eq2JSZTQyMhg==";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="jwtHelper"></param>
        /// <param name="authorizationService"></param>
        /// <param name="httpContextAccessor"></param>
        public AuthorizationController(JwtHelper jwtHelper,
            //IAuthorizatoinsService authorizationService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthorizationController> logger)
        {
            _jwtHelper = jwtHelper;
            //_authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }


        /// <summary>
        /// 密码明文RSA SHA256 加密
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost, AllowAnonymous]// AllowAnonymous：允许匿名访问
        public BaseResp<string> GetRSAPwd(string req)
        {
            BaseResp<string> resp = new();
            resp.ResultCode = 1;
            resp.ResultMsg = "失败";
            try
            {
                var rsa = new RSAHelper(RSAType.RSA2, Encoding.UTF8, privateKey, publicKey);
                var pwd = rsa.Encrypt(req);
                return ApiResult.SetSuccess<string>("加密成功", pwd);
            }
            catch (Exception ex)
            {
                return ApiResult.SetFailure(ex.Message, "");
            }
        }

        /// <summary>
        /// 生成token
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost, AllowAnonymous]
        public BaseResp<string> GetToken([FromBody] AuthorizationModel req)
        {
            BaseResp<string> resp = new();
            resp.ResultCode = 1;
            resp.ResultMsg = "账号/密码错误";
            try
            {
                //WxPayData wx = new WxPayData(_logger);
                //wx.SetValue("userName", req.UserName);
                //wx.SetValue("pwd", req.Pwd);
                //wx.SetValue("nonce_str", req.nonce_str);

                //var localSign = wx.MakeSignForGetToken();
                //string? headerSign = _httpContextAccessor?.HttpContext?.Request.Headers["sign"].FirstOrDefault();
                //if (string.IsNullOrEmpty(headerSign))
                //    return ApiResult.SetFailure("签名不能为空!", "");

                //if (localSign != headerSign)
                //    return ApiResult.SetFailure("签名验证不通过", "");

                var rsa = new RSAHelper(RSAType.RSA2, Encoding.UTF8, privateKey, publicKey);
                var pwd = rsa.Decrypt(req.Pwd);

                if (req.UserName != "admin" && pwd != "123456")
                    return ApiResult.SetFailure("账号/密码错误", "");

                TokenModelJwt model = new TokenModelJwt();
                model.UserId = 1;
                model.Role = "admin";
                model.UserName = "admin";
                return ApiResult.SetSuccess<string>("获取Token成功", _jwtHelper.CreateToken(model));
            }
            catch (Exception ex)
            {
                return ApiResult.SetFailure(ex.Message, "");
            }
        }
    }
}
