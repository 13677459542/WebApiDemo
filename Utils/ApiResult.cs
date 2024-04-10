using WebApiDemo.Model.Resp;

namespace WebApiDemo.Utils
{
    /// <summary>
    /// API数据返回工具类
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// 成功响应
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static BaseResp<T> SetSuccess<T>(string msg, T t)
        {
            return new BaseResp<T>
            {
                ResultCode = 0,
                ResultMsg = msg,
                Records = new List<T> { t }
            };
        }

        /// <summary>
        /// 设置成功响应
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static BaseResp<T> SetSuccess<T>(T t, string msg) where T : List<T>
        {
            return new BaseResp<T>
            {
                ResultCode = 0,
                ResultMsg = msg,
                Records = t
            };
        }

        /// <summary>
        /// 失败响应
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static BaseResp<T> SetFailure<T>(string msg, T data = default)
        {
            return new BaseResp<T>
            {
                ResultCode = 1,
                ResultMsg = msg,
                Records = data == null ? new List<T>() : new List<T> { data }
            };
        }

        /// <summary>
        /// 异常响应
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static BaseResp<string> SetException(string msg, string data)
        {
            return new BaseResp<string>
            {
                ResultCode = 2,
                ResultMsg = msg,
                Records = new List<string> { data }
            };
        }
    }
}
