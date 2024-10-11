using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using WebApiDemo.Utils;
using WebApiDemo.Utils.LogMiddleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// 1、添加中间件支持跨域
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("*") // 替换为允许访问的域名，*代表允许所有域请求
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowAnyOrigin();
               //.AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

builder.UseSerilogExtensions();// 配置Serilog日志

builder.Services.UseJWTMiddlewareExtensions(builder); // 注入JWT扩展

builder.Services.AddSwaggerGen(options =>
{
    options.UseSwaggerJoinBearer();// Swagger配置支持Token参数传递

    //options.SwaggerDoc("v1", new OpenApiInfo
    //{
    //    Title = "WebApiDemo接口",
    //    Version = "V1.0.0",
    //    Description = "WebApiDemo_WebAPI"
    //});

    #region 接口版本分组（第一步）
    foreach (FieldInfo field in typeof(ApiVersioninfo).GetFields())
    {
        options.SwaggerDoc(field.Name, new OpenApiInfo()
        {
            Title = $"{field.Name}版本 dotnet core webapi",
            Version = field.Name,
            Description = $"{field.Name}版本"
        });
    }
    #endregion

    #region 显示接口注释信息
    var file = Path.Combine(AppContext.BaseDirectory, "WebApiDemo.xml");  // xml文档绝对路径，文件名称设置为项目名称
    var path = Path.Combine(AppContext.BaseDirectory, file); // xml文档绝对路径，需要先在 项目右键属 > 生成 > 文档文件 > 勾选生成包含API文档的文件
    options.IncludeXmlComments(path, true); // true : 显示控制器层注释
    #endregion
    options.OrderActionsBy(o => o.RelativePath); // 对action的名称进行排序，如果有多个，就可以看见效果了。
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    //app.UseSwaggerUI();
    #region 接口版本分组（第二步）
    app.UseSwaggerUI(options =>
    {
        foreach (FieldInfo field in typeof(ApiVersioninfo).GetFields())
            options.SwaggerEndpoint($"/swagger/{field.Name}/swagger.json", $"版本选择：{field.Name}");
    });
    #endregion

}

app.UseRequestResponseLogging();// 使用日志中间件

app.UseCors(); // 2、跨域中间件生效

//app.UseHttpsRedirection();
app.UseAuthentication();// 鉴权 启用JWT Token认证 注意顺序一定是先认证(UseAuthentication)后授权(UseAuthorization) 不然接口即使附加token也认证不通过
app.UseAuthorization();// 授权

app.MapControllers();

app.Run();
