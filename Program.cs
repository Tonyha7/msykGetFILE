using Aliyun.OSS;
using Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace msykGetFILE
{
    internal class Program
       
    { 
        static void Main(string[] args)
        { Console.ForegroundColor = ConsoleColor.Green;
            String endpoint = "https://oss-cn-shanghai.aliyuncs.com";
            String msykkey = "DxlE8wwbZt8Y2ULQfgGywAgZfJl82G9S";
            String SecurityToken, AccessKeyId, AccessKeySecret = "";
            string maker = string.Empty;
            var flag = true;
            int k = 0;
            Console.WriteLine("Made by th7 20221017");
            Console.WriteLine("鍵入下載目錄（如F:/）:");
            String pan = Console.ReadLine();
            while (true)
            {
            String timee = GetTimeStamp();
            String keyy = Md5Func(timee + msykkey);
            Dictionary<string, string> canshu = new Dictionary<string, string>();
            canshu.Add("salt", timee);
            canshu.Add("key", keyy);
            String raw = Post("https://padapp.msyk.cn/ws/common/uploadController/getParams", canshu);
            Root rt = JsonConvert.DeserializeObject<Root>(raw);

            if (rt.code == "10000")
            {
                SecurityToken = rt.SecurityToken;
                AccessKeyId = rt.AccessKeyId;
                AccessKeySecret = rt.AccessKeySecret;
                Console.WriteLine("Params成功");
                OssClient client = new OssClient(endpoint, AccessKeyId, AccessKeySecret, SecurityToken);
                Console.WriteLine("鍵入日期（如20221017）:");
                String day = Console.ReadLine();
                do
                {
                    var listObjectsRequest = new ListObjectsRequest("msyk");
                listObjectsRequest.Prefix = "squirrel/material/" + day + "/"; //指定下一级文件
                listObjectsRequest.Marker = maker; //获取下一页的起始点，它的下一项
                listObjectsRequest.MaxKeys = 100;//设置分页的页容量
                listObjectsRequest.Delimiter = "/";//跳出递归循环，只去指定目录下的文件。使用它时 Prefix文件路径要以“/”结尾
                var listResult = client.ListObjects(listObjectsRequest);
                foreach (var summary in listResult.ObjectSummaries)
                {
                    if (summary.Key.EndsWith(".pdf") || summary.Key.EndsWith(".PDF"))
                    {
                        Console.WriteLine("開始下載:"+summary.Key);
                            try { 
                            var obj = client.GetObject("msyk", summary.Key);
                                String path = pan+summary.Key.Substring(0,summary.Key.Length-37);
                                if (!Directory.Exists(path))
                                { Directory.CreateDirectory(path); }
                                using (var requestStream = obj.Content)
                            {
                                byte[] buf = new byte[1024];
                                var fs = File.Open(pan+ summary.Key, FileMode.OpenOrCreate);
                                var len = 0;
                                while ((len = requestStream.Read(buf, 0, 1024)) != 0)
                                {
                                    fs.Write(buf, 0, len);
                                }

                                fs.Close();
                                Console.WriteLine("下載完成:" + summary.Key);
                                }
                            }catch (Exception ex)
                            {
                                Console.WriteLine("下載失敗:" + summary.Key+ex);
                            }
                            
                        }
                        k++;
                    }
                    maker = listResult.NextMarker;
                    flag = listResult.IsTruncated;//全部执行完后，为false
                } while (flag);
                Console.WriteLine("Done.");
            }
            else
            {
                Console.WriteLine("Params失敗");
            }

            Console.ReadLine();
            }
            
        }

        /// <summary>
        /// 指定Post地址使用Get 方式获取全部字符串
        /// </summary>
        /// <param name="url">请求后台地址</param>
        /// <returns></returns>
        public static string Post(string url, Dictionary<string, string> dic)
        {
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            #region 添加Post 参数
            StringBuilder builder = new StringBuilder();
            int i = 0;
            foreach (var item in dic)
            {
                if (i > 0)
                    builder.Append("&");
                builder.AppendFormat("{0}={1}", item.Key, item.Value);
                i++;
            }
            byte[] data = Encoding.UTF8.GetBytes(builder.ToString());
            req.ContentLength = data.Length;
            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();
            }
            #endregion
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            //获取响应内容
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }

        /// <summary>
        /// 获得13位的时间戳
        /// </summary>
        /// <returns></returns>
        public static string GetTimeStamp()
        {
            System.DateTime time = System.DateTime.Now;
            long ts = ConvertDateTimeToInt(time);
            return ts.ToString();
        }
        /// <summary>  
        /// 将c# DateTime时间格式转换为Unix时间戳格式  
        /// </summary>  
        /// <param name="time">时间</param>  
        /// <returns>long</returns>  
        private static long ConvertDateTimeToInt(System.DateTime time)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            long t = (time.Ticks - startTime.Ticks) / 10000;   //除10000调整为13位      
            return t;
        }

        /// </summary>
        public static string Md5Func(string source)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.UTF8.GetBytes(source);
            byte[] md5Data = md5.ComputeHash(data, 0, data.Length);
            md5.Clear();

            string destString = string.Empty;
            for (int i = 0; i < md5Data.Length; i++)
            {
                //返回一个新字符串，该字符串通过在此实例中的字符左侧填充指定的 Unicode 字符来达到指定的总长度，从而使这些字符右对齐。
                // string num=12; num.PadLeft(4, '0'); 结果为为 '0012' 看字符串长度是否满足4位,不满足则在字符串左边以"0"补足
                //调用Convert.ToString(整型,进制数) 来转换为想要的进制数
                destString += System.Convert.ToString(md5Data[i], 16).PadLeft(2, '0');
            }
            //使用 PadLeft 和 PadRight 进行轻松地补位
            destString = destString.PadLeft(32, '0');
            return destString;
        }
    }
}


