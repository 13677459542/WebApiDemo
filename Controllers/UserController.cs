using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WebApiDemo.Model.Resp;
using WebApiDemo.Utils;
using WebApiDemo.Utils.Filters;

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
        /// 角色授权登录
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
        public BaseResp<string> RolesLogin(string req)
        {
            // AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme ： 必须是JWT鉴权方式
            // Roles = "admin" ：角色授权，指定角色只能访问
            // 场景一：如果要验证同时具备多个role,就标记多个 Authorize，分别把角色写上。 [Authorize(Roles = "admin")]  [Authorize(Roles = "test")] ...
            // 场景二：多个角色，只要有一个角色匹配即可，Roles = "admin,test,..."  使用  ,  分隔
            return ApiResult.SetSuccess<string>("登录成功", req);
        }

        /// <summary>
        /// 策略授权登录
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Policy001")]
        public BaseResp<string> PolicyLogin(string req)
        {
            return ApiResult.SetSuccess<string>("登录成功", req);
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost]
        //[CustomCorsActionFilterAttribute] // 标记该方法支持跨域请求
        [ApiExplorerSettings(GroupName = nameof(ApiVersioninfo.WebApiDemo_V1))]
        public BaseResp<string> Test1(string req)
        {
            // HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");// 单个接口允许所有域的请求
            return ApiResult.SetSuccess<string>("获取成功", req);
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost]
        [ApiExplorerSettings(GroupName = nameof(ApiVersioninfo.WebApiDemo_V2))]
        public BaseResp<string> Test2(string req)
        {
            return ApiResult.SetSuccess<string>("获取成功", req);
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost]
        [ApiExplorerSettings(GroupName = nameof(ApiVersioninfo.WebApiDemo_V3))]
        public BaseResp<string> Test3(string req)
        {
            return ApiResult.SetSuccess<string>("获取成功", req);
        }

        /// <summary>
        /// JSONP请求方法
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [CustomJSONPFilterAttribute]
        public BaseResp<string> GetJSONPCrossDaminDataNoParaameeter()
        {
            return ApiResult.SetSuccess<string>("获取成功", "请求成功！");
        }

        /// <summary>
        /// 获取图片转文件流并输出
        /// </summary>
        /// <returns></returns>
        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> GetImageStream()
        {
            MemoryStream stream;
            using(FileStream fs =new FileStream("Images/banner.png", FileMode.Open))
            {
                int len = (int)fs.Length;
                byte[] buf = new byte[len];
                fs.Read(buf, 0, len);
                stream = new MemoryStream();
                stream.Write(buf, 0, len);
            }
            return await Task.FromResult<FileResult>(File(stream.ToArray(),"image/gif"));
        }
    }
}
