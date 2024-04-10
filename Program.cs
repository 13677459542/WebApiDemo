using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Text;
using WebApiDemo.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//跨域
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("*") // 替换为允许访问的域名
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

#region JWT验证
// 1、安装包：Microsoft.AspNetCore.Authentication.JwtBearer
// 2、添加app使用中间件  app.UseAuthentication();
// 3、注册JWT服务
// 4、配置Swagger全局授权.在builder.Services.AddSwaggerGen节点下创建 AddSecurityDefinition、AddSecurityRequirement

var configuration = builder.Configuration;
builder.Services.AddSingleton(new AppSettings(configuration));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
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
            string result = JsonConvert.SerializeObject(new { resutCode = 403, resutMsg = "禁止访问服务" });
            context.Response.WriteAsync(result);
            return Task.FromResult(0);
        }
    };
});

builder.Services.AddSingleton(new JwtHelper(configuration));
#endregion

builder.Services.AddSwaggerGen(options =>
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
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WebApiDemo接口",
        Version = "V1.0.0",
        Description = "WebApiDemo_WebAPI"
    });
    var file = Path.Combine(AppContext.BaseDirectory, "WebApiDemo.xml");  // xml文档绝对路径
    var path = Path.Combine(AppContext.BaseDirectory, file); // xml文档绝对路径，需要先在 项目右键属 > 生成 > 文档文件 > 勾选生成包含API文档的文件
    options.IncludeXmlComments(path, true); // true : 显示控制器层注释
    options.OrderActionsBy(o => o.RelativePath); // 对action的名称进行排序，如果有多个，就可以看见效果了。
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseRequestResponseLogging();
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();// 启用JWT Token认证 注意顺序一定是先认证(UseAuthentication)后授权(UseAuthorization) 不然接口即使附加token也认证不通过
app.UseAuthorization();

app.MapControllers();

app.Run();
