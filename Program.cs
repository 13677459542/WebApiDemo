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

//����
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("*") // �滻Ϊ�������ʵ�����
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

#region JWT��֤
// 1����װ����Microsoft.AspNetCore.Authentication.JwtBearer
// 2������appʹ���м��  app.UseAuthentication();
// 3��ע��JWT����
// 4������Swaggerȫ����Ȩ.��builder.Services.AddSwaggerGen�ڵ��´��� AddSecurityDefinition��AddSecurityRequirement

var configuration = builder.Configuration;
builder.Services.AddSingleton(new AppSettings(configuration));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true, //�Ƿ���֤Issuer
        ValidIssuer = configuration["Jwt:Issuer"], //������Issuer
        ValidateAudience = true, //�Ƿ���֤Audience
        ValidAudience = configuration["Jwt:Audience"], //������Audience
        ValidateIssuerSigningKey = true, //�Ƿ���֤SecurityKey
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"])), //SecurityKey
        ValidateLifetime = true, //�Ƿ���֤ʧЧʱ��
        ClockSkew = TimeSpan.FromSeconds(30), //����ʱ���ݴ�ֵ�������������ʱ�䲻ͬ�����⣨�룩
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
            string result = JsonConvert.SerializeObject(new { resutCode = 401, resutMsg = "�ӿ�δ��Ȩ,���¼��Ȩ�����ȡToken" });
            context.Response.WriteAsync(result);
            return Task.FromResult(0);
        },
        //403
        OnForbidden = context =>
        {
            context.Response.ContentType = "text/plain";
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            string result = JsonConvert.SerializeObject(new { resutCode = 403, resutMsg = "��ֹ���ʷ���" });
            context.Response.WriteAsync(result);
            return Task.FromResult(0);
        }
    };
});

builder.Services.AddSingleton(new JwtHelper(configuration));
#endregion

builder.Services.AddSwaggerGen(options =>
{
    #region Swagger����֧��Token�������� (swagger�����һ����Ȩ��С��)
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Description = "���¿�����������ͷ����Ҫ����Jwt��ȨToken��Bearer Token ��ע���м�����пո�",
        Name = "Authorization",// ������ Authorization ������swagger����֤����ᱨ��
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
        Title = "WebApiDemo�ӿ�",
        Version = "V1.0.0",
        Description = "WebApiDemo_WebAPI"
    });
    var file = Path.Combine(AppContext.BaseDirectory, "WebApiDemo.xml");  // xml�ĵ�����·��
    var path = Path.Combine(AppContext.BaseDirectory, file); // xml�ĵ�����·������Ҫ���� ��Ŀ�Ҽ��� > ���� > �ĵ��ļ� > ��ѡ���ɰ���API�ĵ����ļ�
    options.IncludeXmlComments(path, true); // true : ��ʾ��������ע��
    options.OrderActionsBy(o => o.RelativePath); // ��action�����ƽ�����������ж�����Ϳ��Կ���Ч���ˡ�
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
app.UseAuthentication();// ����JWT Token��֤ ע��˳��һ��������֤(UseAuthentication)����Ȩ(UseAuthorization) ��Ȼ�ӿڼ�ʹ����tokenҲ��֤��ͨ��
app.UseAuthorization();

app.MapControllers();

app.Run();