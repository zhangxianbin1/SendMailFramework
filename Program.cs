using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Winista.Text.HtmlParser;
using Winista.Text.HtmlParser.Filters;
using Winista.Text.HtmlParser.Util;

namespace SendMailFramework
{
    class Program
    {
        static void Main(string[] args)
        {
            //通过计划任务程序定时发送或者在程序中定时发送
            //Thread RunHourThread = new Thread(new ThreadStart(RunHour));
            //RunHourThread.Start();
            Basic();
        }
        public static void Basic()
        {
            //需要获取的故事的num,因为从0开始的，所以这个num也是已获取的故事数目
            int curnum = int.Parse(ConfigurationManager.AppSettings.Get("curnum"));
            string url, content = "", href = "", storytitle = "";
            while ((href == "" || storytitle == "" || content == "") && curnum < 4000)
            {
                int pageIndex = curnum / 70 + 1;
                if (pageIndex == 1)
                {
                    url = "http://www.tom61.com/ertongwenxue/shuiqiangushi/index.html";
                }
                else
                {
                    url = "http://www.tom61.com/ertongwenxue/shuiqiangushi/index_" + pageIndex + ".html";
                }
                string HtmlString = HttpGet(url, "");
                parseIndexHtml(HtmlString, curnum % 70, out href, out storytitle);
                curnum++;
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["curnum"].Value = curnum.ToString();
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                content = parseHtml("http://www.tom61.com" + href);
            }
            sendmail(content, storytitle);
            Console.WriteLine(DateTime.Now + "发送邮件完成！");
            Console.ReadKey();
        }
        public static void RunHour()
        {
            while (true)
            {
                try
                {
                    if (DateTime.Now.Hour == 9)
                    {
                        Basic();
                        Thread.Sleep(1800000);//30分钟
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    Thread.Sleep(30000);//30秒
                }
            }
        }
        public static void parseIndexHtml(string HtmlString, int num, out string href, out string title)
        {
            href = "";
            title = "";
            //进行解析
            Parser parser = Parser.CreateParser(HtmlString, "utf-8");
            //筛选要查找的对象 这里查找td，封装成过滤器
            NodeFilter filter = new TagNameFilter("dd");
            new AndFilter(new TagNameFilter("dd"), new HasParentFilter(new AndFilter(new TagNameFilter("dl"), new HasAttributeFilter("class", "txt_box"))));
            //将过滤器导入筛选，得到对象列表
            NodeList nodes = parser.Parse(filter);
            if (nodes.Size() > num)
            {
                INode textnode = nodes[num];
                ITag tag = getTag(textnode.FirstChild.NextSibling);
                href = tag.GetAttribute("href");
                title = tag.GetAttribute("title");
            }
        }
        public static string parseHtml(string url)
        {
            string result = "";
            string HtmlString = HttpGet(url, "");
            //进行解析
            Parser parser = Parser.CreateParser(HtmlString, "utf-8");
            //筛选要查找的对象 这里查找td，封装成过滤器
            NodeFilter filter = new AndFilter(new TagNameFilter("p"), new HasParentFilter(new AndFilter(new TagNameFilter("div"), new HasAttributeFilter("class", "t_news_txt"))));
            //将过滤器导入筛选，得到对象列表
            NodeList nodes = parser.Parse(filter);
            for (int i = 0; i < nodes.Size(); i++)
            {
                INode textnode = nodes[i];
                result += (result == "" ? "" : "\r\n") + textnode.ToPlainTextString().Replace("&quot;","\"");
            }
            return result;
        }
        private static ITag getTag(INode node)
        {
            if (node == null)
                return null;
            return node is ITag ? node as ITag : null;
        }
        public static string HttpGet(string Url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }
        public static void sendmail(string msg, string title)
        {
            using (MailMessage mailmessage = new MailMessage())
            using (SmtpClient smtpclient = new SmtpClient("smtp.163.com"))
            {
                //填补邮件信息
                mailmessage.To.Add("sunppazsj@163.com");
                //mailmessage.To.Add(diz);//可以有多个
                mailmessage.Body = msg;
                mailmessage.From = new MailAddress("xianbin1016@163.com");
                mailmessage.Subject = title;
                smtpclient.UseDefaultCredentials = true;
                smtpclient.EnableSsl = true;
                smtpclient.Credentials = new System.Net.NetworkCredential("xianbin1016", "zxb2872067");
                smtpclient.Send(mailmessage);//发送
            }
        }
    }
}
