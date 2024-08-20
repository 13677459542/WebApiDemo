using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace WebApiDemo.Utils
{
    public class NickAuthorizationHandler : AuthorizationHandler<CustomNickNameRequirement>
    {
        public NickAuthorizationHandler()
        {
            // 这里可以引入第三方服务来取验证数据或方法，并在HandleRequirementAsync方法中去写验证逻辑
        }


        /// <summary>
        /// 可以再这里写入验证逻辑
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requirement"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CustomNickNameRequirement requirement)
        {
            if (context.User.Claims.Count() == 0)
            {
                return Task.CompletedTask;// 验证失败
            }

            string nickName = context.User.Claims.FirstOrDefault(c => c.Type == "NickName").Value;
            if(nickName == "管理员")
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
