using BiHuaCrawler.Model;
using MongoDB.Driver;
using MongoDB.EF;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BiHuaCrawler
{
    /// <summary>
    /// 单个爬虫工作单元
    /// </summary>
    public class CrawlerWorkItem
    {
        ushort beginUnicode = 0;
        ushort endUnicode = 0;
        MongoDBContext dbContext;
        IMongoCollection<ChineseChar> chineseCharSet;
        IMongoCollection<FailChar> failCharSet;
        IMongoCollection<CrawlerInfo> crawlerInfoSet;
        CrawlerInfo crawlerInfo;
        HttpClient httpClient;
        Uri uri;
        int len1, len2;

        public CrawlerWorkItem(MongoDBContext dbContext, string url, ushort beginUnicode, ushort endUnicode)
        {
            this.beginUnicode = beginUnicode;
            this.endUnicode = endUnicode;
            this.dbContext = dbContext;
            this.httpClient = new HttpClient();
            this.httpClient.MaxResponseContentBufferSize = 256000;
            this.httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/36.0.1985.143 Safari/537.36");
            uri = new Uri(url);
            len1 = "<script type='text/javascript'>window.parent.H.app.Chinese.callback(".Length;
            len2 = ");</script>".Length;

            this.chineseCharSet = this.dbContext.DbSet<ChineseChar>();
            this.failCharSet = this.dbContext.DbSet<FailChar>();
            this.crawlerInfoSet = this.dbContext.DbSet<CrawlerInfo>();
        }

        public Task GetTask()
        {
            Task task = Task.Run(async () =>
            {
                await Init();
                ushort begIndex = crawlerInfo.Unicode == 0 ? beginUnicode : crawlerInfo.Unicode;
                if (crawlerInfo.IsCompleted)
                    return;

                string text = string.Empty;

                if (chineseCharSet.Any(a => a.Unicode == begIndex))
                    begIndex++;

                for (ushort i = begIndex; i <= endUnicode; ++i)
                {
                    text = Convert.ToChar(i).ToString();
                    try
                    {
                        ChineseChar chineseChar = await GetOneChinese(httpClient, uri, i, len1, len2);
                        if (chineseChar == null)
                        {
                            PrintFail(0, i, text);
                            AddFailChar(failCharSet, 0, i, text, "");
                            continue;
                        }

                        chineseCharSet.Add(chineseChar);
                        crawlerInfo.Unicode = i;
                        crawlerInfo.ModifiedTime = DateTime.Now;
                        crawlerInfoSet.Update(crawlerInfo);
                        Console.WriteLine($"完成记录 Unicode编码：{i} 字符内容：{text}");
                    }
                    catch (Exception ex)
                    {
                        PrintFail(1, i, text);
                        AddFailChar(failCharSet, 1, i, text, ex.Message);
                    }
                    finally
                    {
                        await Task.Delay(200);
                    }
                }

            }).ContinueWith(t =>
            {
                var foregroundColor = ConsoleColor.White;
                if (t.IsCompletedSuccessfully == false)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(t.Exception.ToString());
                    Console.ForegroundColor = foregroundColor;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{beginUnicode}~{endUnicode} 爬虫完成");
                Console.ForegroundColor = foregroundColor;

                httpClient.Dispose();
                if (t.IsCompletedSuccessfully)
                {
                    crawlerInfo.IsCompleted = true;
                    crawlerInfoSet.Update(crawlerInfo);
                }
                t.Dispose();

            });

            return task;
        }

        public static void AddFailChar(IMongoCollection<FailChar> failCharSet, int failCode, ushort unicode, string text, string failMessage)
        {
            FailChar failChar = null;
            if ((failChar = failCharSet.FirstOrDefault(f => f.Unicode == unicode)) != null)
            {
                failChar.FailCode = failCode;
                failChar.FailMessage = failMessage;
                failChar.ModifiedTime = DateTime.Now;
                failCharSet.Update(failChar);
            }
            else
            {
                failCharSet.Add(new FailChar
                {
                    FailCode = 1,
                    Unicode = unicode,
                    Text = text,
                    FailMessage = failMessage,
                    CreatedTime = DateTime.Now,
                    ModifiedTime = DateTime.Now,
                });
            }
        }

        public static void PrintFail(int type, ushort unicode, string text)
        {
            var foregroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"错误类型：{type} Unicode编码：{unicode} 字符内容：{text}");
            Console.ForegroundColor = foregroundColor;
        }

        public static async Task<ChineseChar> GetOneChinese(HttpClient httpClient, Uri uri, ushort unicode, int len1, int len2)
        {
            JObject jObject = null;
            string text = Convert.ToChar(unicode).ToString();
            using (var formData = new MultipartFormDataContent())
            {
                formData.Add(new StringContent(text), "word");//提交单个字的表单数据
                using (HttpResponseMessage response = (await httpClient.PostAsync(uri, formData)).EnsureSuccessStatusCode())
                {
                    string str = await response.Content.ReadAsStringAsync();
                    var mem = str.AsMemory().Slice(len1, str.Length - len1 - len2);
                    jObject = JObject.Parse(mem.ToString());
                    if (jObject["result"].Value<string>() != "1") //获取成功时返回1
                    {
                        return null;
                    }
                }
            }

            ChineseChar ret = new ChineseChar();
            var json = jObject["chinese"];

            ret.Unicode = unicode;
            ret.Text = text;
            ret.RectSize = 760;
            ret.BuShou = json["bushou"].Value<string>();
            ret.Pinyins = json["pinyin"].Value<string>().Split(',', StringSplitOptions.RemoveEmptyEntries);
            ret.BiShuns = json["bishun"].ToObject<IEnumerable<IEnumerable<int[]>>>();
            ret.BiHuas = json["bihua"].ToObject<IEnumerable<IEnumerable<int[]>>>();
            ret.ModifiedTime = ret.CreatedTime = DateTime.Now;

            return ret;
        }

        async Task Init()
        {
            await Task.Delay(100);
            crawlerInfo = crawlerInfoSet.FirstOrDefault(a => a.BeginUnicode == beginUnicode && a.EndUnicode == endUnicode);
            if (crawlerInfo == null)
            {
                crawlerInfo = new CrawlerInfo
                {
                    BeginUnicode = beginUnicode,
                    EndUnicode = endUnicode,
                    Unicode = 0,
                    ModifiedTime = DateTime.Now,
                };
                crawlerInfoSet.Add(crawlerInfo);
            }
        }


    }
}
