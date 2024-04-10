using Microsoft.Extensions.Configuration.Json;

namespace WebApiDemo.Utils
{
    /// <summary>
    /// 读取配置
    /// </summary>
    public class AppSettings
    {
        static IConfiguration Configuration { get; set; }
        static string contentPath { get; set; }

        public AppSettings(string contentPath)
        {
            string Path = "appsettings.json";
            Configuration = new ConfigurationBuilder()
                .SetBasePath(contentPath)
                .Add(new JsonConfigurationSource
                {
                    Path = Path,
                    Optional = false,
                    ReloadOnChange = true
                }).Build();
        }

        public AppSettings(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        /// <summary>
        /// 封装要操作的字符
        /// </summary>
        /// <param name="sections">节点配置</param>
        /// <returns></returns>
        public static string ReadAppSettings(params string[] sections)
        {
            try
            {

                if (sections.Any())
                {
                    return Configuration[string.Join(":", sections)];
                }
            }
            catch (Exception)
            {

            }

            return "";
        }

        /// <summary>
        /// 递归获取配置信息数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sections"></param>
        /// <returns></returns>
        public static List<T> ReadAppSettings<T>(params string[] sections)
        {
            List<T> list = new List<T>();
            Configuration.Bind(string.Join(":", sections), list);
            return list;
        }
    }
}
