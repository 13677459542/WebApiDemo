using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApiDemo.Utils.Filters
{
    /// <summary>
    /// 标记某个控制器、方法支持跨域请求，也可以全局注册：builder.Services.AddControllers(optons=>{ optons.Filters.Add<CustomCorsActionFilterAttribute>(); })
    /// </summary>
    public class CustomCorsActionFilterAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {

        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            context.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        }
    }
}
