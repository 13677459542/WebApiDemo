using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApiDemo.Utils.LogMiddleware;
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
                new Claim(ClaimTypes.Name,model.UserName),
                //new Claim(ClaimTypes.Role,model.Role),
                //new Claim("Name",model.UserName),
                //new Claim("Role",model.Role),
                new Claim("UserId",model.UserId.ToString()),
                new Claim("NickName",model.NickName),
                new Claim("Description",model.Description)
            };
            // 可以将一个用户的多个角色全部赋予；
            if (!string.IsNullOrWhiteSpace(model.Role))
            {
                claims.AddRange(model.Role.Split(',').Select(s => new Claim(ClaimTypes.Role, s)));
                //claims.Add(new Claim("Role", model.Role));
            }
            // 准备加密key
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            // Sha256 加密方式
            var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
            var jwtSecurityToken = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],     //Issuer
                _configuration["Jwt:Audience"],   //Audience
                claims,                           //Claims,
                DateTime.Now,                     //notBefore
                DateTime.Now.AddMinutes(5),      //expires 到期时间
                signingCredentials                //Credentials 指定的加密方式
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

    /// <summary>
    /// 扩展中间件
    /// </summary>
    public static class JWTMiddlewareExtensions
    {
        /// <summary>
        /// 注入JWT扩展
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static void UseJWTMiddlewareExtensions(this IServiceCollection Services, WebApplicationBuilder builder)
        {
            #region 策略授权扩展Requirement 注入服务
            //Services.AddTransient<服务, 服务>(); // 需要使用的第三方服务注入
            Services.AddTransient<IAuthorizationHandler, NickAuthorizationHandler>();
            #endregion

            #region JWT验证
            // 1、安装包：Microsoft.AspNetCore.Authentication.JwtBearer
            // 2、添加app使用鉴权  app.UseAuthentication();
            // 3、注册JWT服务
            // 4、配置Swagger全局授权.在builder.Services.AddSwaggerGen节点下创建 AddSecurityDefinition、AddSecurityRequirement

            var configuration = builder.Configuration;
            Services.AddSingleton(new AppSettings(configuration));

            Services
                .AddAuthorization(options =>
                {
                    #region 策略授权
                    // 策略授权定义
                    options.AddPolicy("Policy001", policyBuilder =>
                    {
                        // 必须包含什么
                        policyBuilder.RequireRole("admin");
                        policyBuilder.RequireUserName("admin");
                        policyBuilder.RequireClaim("NickName");

                        policyBuilder.RequireAssertion(context =>
                        {
                            // 这里可以写逻辑判断
                            bool bResult = context.User.HasClaim(c => c.Type == ClaimTypes.Role) && context.User.Claims.FirstOrDefault(c => c.Type.Equals(ClaimTypes.Role)).Value == "admin" && context.User.Claims.Any(c => c.Type == ClaimTypes.Name);
                            return bResult;
                        });
                    });

                    #region 策略授权扩展Requirement
                    //Requirement:还是基于策略的授权，Requirement可以把验证逻辑给封装出来
                    // 1、定义Requirement
                    // 2、实现lAuthorizationHandler…直接使用AuthorizationHandler<> 泛型类
                    options.AddPolicy("Policy002", policyBuilder =>
                    {
                        policyBuilder.AddRequirements(new CustomNickNameRequirement());
                    });
                    #endregion
                    #endregion
                })
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; // 鉴权渠道 
                }).AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true, //是否验证Issuer
                        ValidIssuer = configuration["Jwt:Issuer"], //发行人Issuer
                        ValidateAudience = true, //是否验证Audience
                        ValidAudience = configuration["Jwt:Audience"], //订阅人Audience
                        ValidateIssuerSigningKey = true, //是否验证SecurityKey
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"])), //SecurityKey
                        ValidateLifetime = true, //是否验证失效时间
                        ClockSkew = TimeSpan.FromSeconds(30), //过期时间容错值，解决服务器端时间不同步问题（秒）
                        RequireExpirationTime = true,

                        AudienceValidator = (m, n, z) =>
                        {
                            // 这里可以写自己定义的验证逻辑
                            //return m != null && m.FirstOrDefault().Equals(builder.Configuration["Jwt:Audience"]);
                            return true;
                        },
                        LifetimeValidator = (notBefore, expires, securityToken, validationParameters) =>
                        {
                            // 自定义校验规则
                            //return notBefore <= DateTime.Now && expires >= DateTime.Now;
                            return true;
                        }
                    };
                    options.Events = new JwtBearerEvents
                    {
                        //401
                        OnChallenge = context =>
                        {
                            context.HandleResponse();
                            context.Response.ContentType = "text/plain";
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            string result = JsonConvert.SerializeObject(new { resutCode = 401, resutMsg = "接口未授权,请登录授权服务获取Token" });
                            context.Response.WriteAsync(result);
                            return Task.FromResult(0);
                        },
                        //403
                        OnForbidden = context =>
                        {
                            context.Response.ContentType = "text/plain";
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            string result = JsonConvert.SerializeObject(new { resutCode = 403, resutMsg = "不允许访问服务" });
                            context.Response.WriteAsync(result);
                            return Task.FromResult(0);
                        }
                    };
                });

            Services.AddSingleton(new JwtHelper(configuration)); // 注入JwtHelper帮助类单例，方便控制器内使用
            #endregion
        }

        /// <summary>
        /// Swagger配置支持Token参数传递
        /// </summary>
        /// <param name="options"></param>
        public static void UseSwaggerJoinBearer(this SwaggerGenOptions options)
        {
            #region Swagger配置支持Token参数传递 (swagger会出现一个授权的小锁)
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
            {
                Description = "在下框中输入请求头中需要添加Jwt授权Token：Bearer Token （注意中间必须有空格）",
                Name = "Authorization",// 必须填 Authorization 否则在swagger中认证请求会报错
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });
            #endregion
        }
    }
}
