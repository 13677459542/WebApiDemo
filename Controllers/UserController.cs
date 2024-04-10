using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WebApiDemo.Model.Resp;
using WebApiDemo.Utils;

namespace WebApiDemo.Controllers
{
    /// <summary>
    /// 用户
    /// </summary>
    [Route("Api/V1/[controller]/[action]")]
    [Authorize]
    [ApiController]
    public class UserController : Controller
    {
        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost]
        public BaseResp<string> GetUserInfo(string req)
        {
            return ApiResult.SetSuccess<string>("获取成功", req);
        }
    }
}
