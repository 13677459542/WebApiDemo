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

// 1������м��֧�ֿ���
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("*") // �滻Ϊ������ʵ�������*������������������
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowAnyOrigin();
               //.AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

builder.UseSerilogExtensions();// ����Serilog��־

builder.Services.UseJWTMiddlewareExtensions(builder); // ע��JWT��չ

builder.Services.AddSwaggerGen(options =>
{
    options.UseSwaggerJoinBearer();// Swagger����֧��Token��������

    //options.SwaggerDoc("v1", new OpenApiInfo
    //{
    //    Title = "WebApiDemo�ӿ�",
    //    Version = "V1.0.0",
    //    Description = "WebApiDemo_WebAPI"
    //});

    #region �ӿڰ汾���飨��һ����
    foreach (FieldInfo field in typeof(ApiVersioninfo).GetFields())
    {
        options.SwaggerDoc(field.Name, new OpenApiInfo()
        {
            Title = $"{field.Name}�汾 dotnet core webapi",
            Version = field.Name,
            Description = $"{field.Name}�汾"
        });
    }
    #endregion

    #region ��ʾ�ӿ�ע����Ϣ
    var file = Path.Combine(AppContext.BaseDirectory, "WebApiDemo.xml");  // xml�ĵ�����·�����ļ���������Ϊ��Ŀ����
    var path = Path.Combine(AppContext.BaseDirectory, file); // xml�ĵ�����·������Ҫ���� ��Ŀ�Ҽ��� > ���� > �ĵ��ļ� > ��ѡ���ɰ���API�ĵ����ļ�
    options.IncludeXmlComments(path, true); // true : ��ʾ��������ע��
    #endregion
    options.OrderActionsBy(o => o.RelativePath); // ��action�����ƽ�����������ж�����Ϳ��Կ���Ч���ˡ�
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    //app.UseSwaggerUI();
    #region �ӿڰ汾���飨�ڶ�����
    app.UseSwaggerUI(options =>
    {
        foreach (FieldInfo field in typeof(ApiVersioninfo).GetFields())
            options.SwaggerEndpoint($"/swagger/{field.Name}/swagger.json", $"�汾ѡ��{field.Name}");
    });
    #endregion

}

app.UseRequestResponseLogging();// ʹ����־�м��

app.UseCors(); // 2�������м����Ч

//app.UseHttpsRedirection();
app.UseAuthentication();// ��Ȩ ����JWT Token��֤ ע��˳��һ��������֤(UseAuthentication)����Ȩ(UseAuthorization) ��Ȼ�ӿڼ�ʹ����tokenҲ��֤��ͨ��
app.UseAuthorization();// ��Ȩ

app.MapControllers();

app.Run();
