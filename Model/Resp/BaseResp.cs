using System.Xml.Serialization;

namespace WebApiDemo.Model.Resp
{
    /// <summary>
    /// 响应基类
    /// </summary>
    //[XmlRoot("Response")]
    public class BaseResp<T>
    {
        /// <summary>
        /// 状态码 0:表示成功 1：表示失败
        /// </summary>
        //[XmlElement("resultCode")]
        public int ResultCode { get; set; }
        /// <summary>
        /// 结果值
        /// </summary>
        //[XmlElement("resultMsg")]
        public string ResultMsg { get; set; }
        /// <summary>
        /// 响应集
        /// </summary>
        //[XmlArray("Records"), XmlArrayItem("Record")]
        public List<T?> Records { get; set; }
    }
}
