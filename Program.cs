using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Text;
using WebApiDemo.Utils;
using WebApiDemo.Utils.LogMiddleware;

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

#region 配置Serilog日志
//日志目录
string infoPath = Directory.GetCurrentDirectory() + @"\Logs\info\.log"; ;
string waringPath = Directory.GetCurrentDirectory() + @"\Logs\waring\.log";
string errorPath = Directory.GetCurrentDirectory() + @"\Logs\error\.log";
string fatalPath = Directory.GetCurrentDirectory() + @"\Logs\fatal\.log";
string template = "{NewLine}时间:{Timestamp:yyyy-MM-dd HH:mm:ss.fff}{NewLine}等级:{Level}{NewLine}来源:{SourceContext}{NewLine}具体消息如下:{Message}{NewLine}{Exception}";
// 配置Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // 排除Microsoft的日志
    .Enrich.FromLogContext() // 注册日志上下文
    .WriteTo.Console(new CompactJsonFormatter()) // 输出到控制台
    .Enrich.FromLogContext()
    .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(lev => lev.Level == LogEventLevel.Information).
     WriteTo.Async(congfig => congfig.File(
               infoPath,
               rollingInterval: RollingInterval.Day,
               fileSizeLimitBytes: 1024 * 1024 * 10,//默認1GB
               retainedFileCountLimit: 10,//保留最近多少個文件  默認31個
               rollOnFileSizeLimit: true,//超過文件大小時 自動創建新文件  
               shared: true,
               outputTemplate: template)
     ))
     .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(lev => lev.Level == LogEventLevel.Warning).
     WriteTo.Async(congfig => congfig.File(
               waringPath,
               rollingInterval: RollingInterval.Day,
               fileSizeLimitBytes: 1024 * 1024 * 10,
               retainedFileCountLimit: 10,
               rollOnFileSizeLimit: true,
               shared: true,
               outputTemplate: template)
     ))
    .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(lev => lev.Level == LogEventLevel.Error).
     WriteTo.Async(congfig => congfig.File(
               errorPath,
               rollingInterval: RollingInterval.Day,
               fileSizeLimitBytes: 1024 * 1024 * 10,
               retainedFileCountLimit: 10,
               rollOnFileSizeLimit: true,
               shared: true,
               outputTemplate: template)
     ))
     .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(lev => lev.Level == LogEventLevel.Fatal).
     WriteTo.Async(congfig => congfig.File(
               fatalPath,
               rollingInterval: RollingInterval.Day,
               fileSizeLimitBytes: 1024 * 1024 * 10,
               retainedFileCountLimit: 10,
               rollOnFileSizeLimit: true,
               shared: true,
               outputTemplate: template)
     ))
     .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(lev => lev.Level == LogEventLevel.Debug).
     WriteTo.Async(congfig => congfig.File(
               fatalPath,
               rollingInterval: RollingInterval.Day,
               fileSizeLimitBytes: 1024 * 1024 * 10,
               retainedFileCountLimit: 10,
               rollOnFileSizeLimit: true,
               shared: true,
               outputTemplate: template)
     ))
      .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(lev => lev.Level == LogEventLevel.Verbose).
     WriteTo.Async(congfig => congfig.File(
               fatalPath,
               rollingInterval: RollingInterval.Day,
               fileSizeLimitBytes: 1024 * 1024 * 10,
               retainedFileCountLimit: 10,
               rollOnFileSizeLimit: true,
               shared: true,
               outputTemplate: template)
     ))
.CreateLogger();
builder.Host.UseSerilog();
#endregion

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

app.UseRequestResponseLogging();// 使用日志中间件
app.UseCors();
//app.UseHttpsRedirection();
app.UseAuthentication();// 启用JWT Token认证 注意顺序一定是先认证(UseAuthentication)后授权(UseAuthorization) 不然接口即使附加token也认证不通过
app.UseAuthorization();

app.MapControllers();

app.Run();
