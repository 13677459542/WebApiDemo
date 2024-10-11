using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.RegularExpressions;
using WebApiDemo.Model;
using WebApiDemo.Model.Resp;
using WebApiDemo.Utils;
using WebApiDemo.Utils.Model;
using System.Net.Mail;
using MimeKit;
using MailKit.Net.Smtp;

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
        private readonly string publicKey = @"MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA3Pg+4SnPGW71Fm3tTxzzb0n1b41Pr1GuXvO4rA1VOX+SrVz+K8g98qyOF6RM2iOdaJBWat6IVjTuTLwVw+m2Q5aHlHSxkCX/RIBJNt9qIaOytbnn711ssecN00W+NFuiotLLeVP+ttSb28HGCXaGvNe1PFswDh0ZJMcMGY8JvC+h135nQNCQ0hG06vj0KzrmeIxRRNX1AQFIQz+7/hy/oJ/o0XJXrgGdH2e5e3Kox260G9QRruQZ4HGMrQ7VvaqeJ1v14qHLVI0ij3ycPeYJQ+tjXfjcheUppaTSuT2pdQRTY97nkPY//e3zXs4MKzlL3GAY+a3CfqjWRSjiOqTSywIDAQAB";
        /// <summary>
        ///  //2048 私钥
        /// </summary>
        private readonly string privateKey = @"MIIEowIBAAKCAQEA3Pg+4SnPGW71Fm3tTxzzb0n1b41Pr1GuXvO4rA1VOX+SrVz+K8g98qyOF6RM2iOdaJBWat6IVjTuTLwVw+m2Q5aHlHSxkCX/RIBJNt9qIaOytbnn711ssecN00W+NFuiotLLeVP+ttSb28HGCXaGvNe1PFswDh0ZJMcMGY8JvC+h135nQNCQ0hG06vj0KzrmeIxRRNX1AQFIQz+7/hy/oJ/o0XJXrgGdH2e5e3Kox260G9QRruQZ4HGMrQ7VvaqeJ1v14qHLVI0ij3ycPeYJQ+tjXfjcheUppaTSuT2pdQRTY97nkPY//e3zXs4MKzlL3GAY+a3CfqjWRSjiOqTSywIDAQABAoIBABGePsU3ztS5or5LEs9qq4OFY2r62rj2dkSz11brKTm7EOLUYJ+fs5tpuVqWiwTJhNTAIrkaonGH1DLiEYoxVDWcsZVbSIe0aon3qzQTfs7NJ9k9crSvFOo1AJvbxQfqVn6iTVQ7J4/u8Q5bK4MNpD3iT7JO7aHycqgasWhISKpsVo7gWRolMf6VXmFOkDFHh8B2Pkcb8yq0duhSvfsVcy1MJq/9dkRqRTiCCZmNZER5ceq/q6xdnEcfjawnZp08lYSVtTe0A2FclYwqPyvmoNDgpCu2nxgxhPFGq3cmzjsKgM5VMoxb51gniG+lQMDmY48OOlCpjw2eRf2UTfBNfiECgYEA71WwnTJ9zlaocx4yCcUYpUId/Z+V3TjAHmqkkDIBuBYdwzsmYoTuTpHDx3NA/NJRegpH720VA0WfeHhRfdqfiGCSbJ3qD7YIB2NCXopadBoqx9FvqVSnABxtfixCmFWipzsFw0k2wOpeJNDk6YcpQyBEu4S3Maj2Bhuk/s7t8ScCgYEA7FsvtY2XFJqJN84aQmV+gVwntZA86UXjaV3ZYErszQIERUMAudIavSOoai5C5KGtq0NxCW7A4bkCDVyIsCwMUmd0M6Be35wriWai2vVu3u/OE/yfqgHajf8DGd3vXyh9o12H5v8envN6Wh/CUGRZf/GEy1GhM+u2bNA8tcuzj70CgYEAnSQwICaEv7PaSitrQ0rr0aXFtz7O0T9vtQjkH+EVi97Jj+QIYetR5LiESTJ9WwJkiLKzZJrEjy9pc1ncd7vRv2NZAIP2qHYmc2NSsmw4075SlHwIyq9QLxx7L7qzxv2DHDX+pKgvkR7QzW9yvXoHN5G6TzzmY27CimQgQ0VuqUUCgYBMv7t5R9X0Uc4W+e0a/Fwc43Ddi03MLe6Pi3MHyqykUXBTkVNOA8S9ADQy7ny4Qyvivg6ZkoY9hdb9wbt9AYCqzX81OHE2ST716gcd9K6g49vWL6UlDl8K1vEJ2EBfdQV/I+L6hoNJ+CQV2dQ+SKerXSDS6NngwzzEjsX3/oJ7PQKBgE/gciY4CuViwM5UeRxZ+kLYPeNnJOnrCj/TthXFkmPxpa/KD9MaBZo8znHDcxRZX1Y/e7Q3yvUYwejOE9jYehieoVEa47WNbnQHFAJCRY6gnnBwro5eLwn4SSGY8+Wu2Dol2lfmAHhVlk0+Zugc3rvPpkF9sKEDzspb94AEtpam";

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
        /// 密码明文RSA2 SHA256 加密
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost, AllowAnonymous]// AllowAnonymous：允许匿名访问
        public BaseResp<string> GetRSAPwd(string req)
        {
            //BaseResp<string> resp = new();
            //resp.ResultCode = 1;
            //resp.ResultMsg = "失败";
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

        // <summary>
        /// 数据签名
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost, AllowAnonymous]// AllowAnonymous：允许匿名访问
        public BaseResp<string> GetSign(string req)
        {
            try
            {
                var rsa = new RSAHelper(RSAType.RSA2, Encoding.UTF8, privateKey, publicKey);
                var pwd = rsa.Sign(req);
                return ApiResult.SetSuccess<string>("签名成功", pwd);
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
                var rsa = new RSAHelper(RSAType.RSA2, Encoding.UTF8, privateKey, publicKey);

                //string? headerSign = _httpContextAccessor?.HttpContext?.Request.Headers["sign"].FirstOrDefault();
                //if (string.IsNullOrEmpty(headerSign))
                //    return ApiResult.SetFailure("签名不能为空!", "");
                //if (rsa.Verify(JsonConvert.SerializeObject(req), headerSign))
                //    return ApiResult.SetFailure("签名验证不通过", "");

                var pwd = rsa.Decrypt(req.Pwd);
                if (req.UserName != "admin" || pwd != "123456")
                    return ApiResult.SetFailure("账号/密码错误", "");

                TokenModelJwt model = new TokenModelJwt()
                {
                    UserId = 1,
                    Role = "admin,doctor,test",//可以同时赋值多个角色
                    UserName = "admin",
                    NickName = "管理员",
                    Description = "管理员获取令牌",
                };
                return ApiResult.SetSuccess<string>("获取Token成功", _jwtHelper.CreateToken(model));
            }
            catch (Exception ex)
            {
                return ApiResult.SetFailure(ex.Message, "");
            }
        }

        /// <summary>
        /// 定义字典存放验证码，建议存放至Redis或数据库中
        /// </summary>
        private readonly static Dictionary<string, int> _maildate = new();
        /// <summary>
        /// 发送邮箱验证码
        /// </summary>
        /// <param name="_mail"></param>
        /// <param name="mails"></param>
        /// <returns></returns>
        [HttpPost, AllowAnonymous]// AllowAnonymous：允许匿名访问
        public BaseResp<string> PostEmails([FromBody] Mail mails)
        {
            try
            {
                string SendMessage = ""; // 接收发送的返回值

                //如果字典中有该邮箱对应的验证码则删除
                if (_maildate.ContainsKey(mails.recipientArry))
                {
                    _maildate.Remove(mails.recipientArry);
                }
                //设置邮箱正则表达式
                string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                Regex regeemail = new Regex(emailPattern);
                if (!regeemail.IsMatch(mails.recipientArry)) return ApiResult.SetFailure("收件人邮箱地址格式错误", "");
               
                // 创建邮件消息
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Your Name", mails.fromPerson)); // 发件人邮箱
                message.To.Add(new MailboxAddress("Recipient Name", mails.recipientArry)); // 收件人邮箱
                message.Subject = mails.mailTitle; // 标题

                //生成四位验证码
                int randomNumber = new Random().Next(1000, 10000);
                //账户与验证码存放在字典中
                if (!string.IsNullOrWhiteSpace(mails.recipientArry))
                {
                    _maildate[mails.recipientArry] = randomNumber;
                }

                if (mails.isbodyHtml)
                {
                    // 添加 HTML 格式的邮件正文
                    message.Body = new TextPart("html")
                    {
                        Text = $"<h1>{mails.mailTitle}</h1><p>你好，你的验证码为：{randomNumber}</p>"
                    };
                }
                else
                {
                    // 添加邮件正文
                    message.Body = new TextPart("plain")
                    {
                        Text = "你好，你的验证码为：" + randomNumber.ToString()
                    };
                }

                //截取发件人邮箱地址从而判断Smtp的值
                string[] sArray = mails.fromPerson.Split(new char[2] { '@', '.' });
                if (sArray[1] == "qq")
                {
                    mails.host = "smtp.qq.com";//如果是QQ邮箱则：smtp.qq.com
                    // 连接到 QQ 邮箱的 SMTP 服务器并发送邮件
                    using (var client = new MailKit.Net.Smtp.SmtpClient())
                    {
                        client.Connect(mails.host, 465, true); // 使用 SSL 连接到 QQ 邮箱的 SMTP 服务器
                        client.Authenticate(mails.fromPerson, mails.code); // 使用 QQ 邮箱的授权码进行身份验证
                        SendMessage = client.Send(message); //发送邮件并接收返回值
                        client.Disconnect(true);
                    }
                }
                else
                {
                    //// 连接到 SMTP 服务器并发送邮件
                    //using (var client = new MailKit.Net.Smtp.SmtpClient())
                    //{
                    //    client.Connect("smtp.example.com", 587, false); // 用你的 SMTP 服务器信息替换
                    //    client.Authenticate("your-email@example.com", "your-password"); // 用你的邮箱账号和密码替换
                    //    client.Send(message);
                    //    client.Disconnect(true);
                    //}
                }

                return ApiResult.SetSuccess<string>("邮箱验证码发送成功！", SendMessage);
            }
            catch (Exception ex)
            {
                return ApiResult.SetException("邮箱验证码发送过程异常！", ex.Message);
            }
        }
    }
}
