using Newtonsoft.Json;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog;
using System.Diagnostics;

namespace WebApiDemo.Utils.LogMiddleware
{
    /// <summary>
    /// 请求日志中间件（无需继承任何基类）
    /// </summary>
    public class RequestLogMiddleware
    {
        //下一个请求委托
        private readonly RequestDelegate _next;
        //通过构造函数完成日志工具对象的注入
        private readonly ILogger<RequestLogMiddleware> _logger;
        private Stopwatch _stopwatch;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="next">请求委托</param>
        /// <param name="logger">日志工具对象</param>
        /// <returns></returns>
        public RequestLogMiddleware(RequestDelegate next, ILogger<RequestLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _stopwatch = new Stopwatch();
        }

        /// <summary>
        /// 公开方法
        /// </summary>
        /// <param name="context">Http请求上下文</param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            _stopwatch.Restart();
            Request req = new Request();
            HttpRequest request = context.Request;
            req.请求地址 = request.Path.ToString();
            req.请求方法 = request.Method;
            req.开始时间 = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");
            // 获取请求body内容
            if (request.Method.ToLower().Equals("post"))
            {
                context.Request.EnableBuffering();
                StreamReader sr2 = new StreamReader(request.Body);
                var body = await sr2.ReadToEndAsync();
                req.请求参数 = body;
                request.Body.Position = 0;
            }
            else if (request.Method.ToLower().Equals("get"))
            {
                req.请求参数 = request.QueryString.Value ?? "";
            }

            // 获取Response.Body内容
            var originalBodyStream = context.Response.Body;

            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;
                await _next(context);
                req.响应参数 = await GetResponse(context.Response);
                req.响应时间 = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");
                await responseBody.CopyToAsync(originalBodyStream);
            }

            // 响应完成记录时间和存入日志
            context.Response.OnCompleted(() =>
            {
                _stopwatch.Stop();
                req.总花费时间 = _stopwatch.ElapsedMilliseconds + "ms";
                var json = JsonConvert.SerializeObject(req, Formatting.Indented);
                _logger.LogInformation(json);
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// 请求体模型
        /// </summary>
        public class Request
        {
            /// <summary>
            /// 请求地址
            /// </summary>
            public string 请求地址 { get; set; }
            /// <summary>
            /// 请求方法
            /// </summary>
            public string 请求方法 { get; set; }
            /// <summary>
            /// 开始时间
            /// </summary>
            public string 开始时间 { get; set; }
            /// <summary>
            ///请求参数 
            /// </summary>
            public string 请求参数 { get; set; }
            /// <summary>
            /// 响应参数
            /// </summary>
            public string 响应参数 { get; set; }
            /// <summary>
            /// 响应时间
            /// </summary>
            public string 响应时间 { get; set; }
            /// <summary>
            /// 总花费时间
            /// </summary>
            public string 总花费时间 { get; set; }
        }

        /// <summary>
        /// 获取响应内容
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public async Task<string> GetResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            return text;
        }
    }

    /// <summary>
    /// 扩展中间件
    /// </summary>
    public static class RequestResponseLoggingMiddlewareExtensions
    {
        /// <summary>
        /// 配置Serilog日志
        /// </summary>
        /// <param name="builder"></param>
        public static void UseSerilogExtensions(this WebApplicationBuilder builder)
        {
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
        }
        /// <summary>
        /// 使用日志中间件
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestLogMiddleware>();
        }
    }
}
