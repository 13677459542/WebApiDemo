namespace WebApiDemo.Model
{
    //所有配置可以写到配置文件中
    public class Mail
    {
        /// <summary>
        /// 发送人邮箱
        /// </summary>
        public string fromPerson = "1834551900@qq.com";

        /// <summary>
        /// 收件人地址
        /// </summary>
        public string recipientArry { get; set; }

        /// <summary>
        /// 抄送地址(抄送邮箱)
        /// </summary>
        public string mailCcArray = "1834551900@qq.com";

        /// <summary>
        /// 标题
        /// </summary>
        public string mailTitle = "Galaxy";

        /// <summary>
        /// 正文
        /// </summary>
        public string? mailBody { get; set; }

        /// <summary>
        /// 客户端授权码(QQ邮箱中获取到的授权码，以QQ邮箱为例子，进入到QQ邮箱网页版当中，点击左上角的设置进入到邮箱设置界面，再次点击账号，然后一直下划找到POP3/IMAP/SMTP/Exchange/CardDAV/CalDAV服务，这里需要将服务开启。) 
        /// </summary>
        public string code = "pxoreashyakzccbb";

        /// <summary>
        /// SMTP邮件服务器
        /// </summary>
        public string? host { get; set; }

        /// <summary>
        /// 正文是否是html格式
        /// </summary>
        public bool isbodyHtml = true;
    }
}
