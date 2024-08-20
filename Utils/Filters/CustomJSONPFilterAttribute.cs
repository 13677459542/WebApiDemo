using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApiDemo.Utils.Filters
{
    /// <summary>
    /// JSONP跨域请求，可标记在接口或控制器上
    /// </summary>
    public class CustomJSONPFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            string callback = context.HttpContext.Request.Query["callback"]; //获取客户端传的参数是否有callback
            if (!string.IsNullOrWhiteSpace(callback))
            {
                ObjectResult result = context.Result as ObjectResult; // 获取返回参数
                context.Result = (new ObjectResult($"{callback}('{result.Value}')")); // 修改返回参数
            }
        }
    }
}
