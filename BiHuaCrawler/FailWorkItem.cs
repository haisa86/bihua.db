using MongoDB.Driver;
using MongoDB.EF;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BiHuaCrawler
{
    public class FailWorkItem
    {
        int? failCode = 0;
        MongoDBContext dbContext;
        IMongoCollection<ChineseChar> chineseCharSet;
        IMongoCollection<FailChar> failCharSet;

        Uri uri;
        int len1, len2;

        public FailWorkItem(MongoDBContext dbContext, string url, int? failCode)
        {
            this.dbContext = dbContext;
            this.failCode = failCode;

            uri = new Uri(url);
            len1 = "<script type='text/javascript'>window.parent.H.app.Chinese.callback(".Length;
            len2 = ");</script>".Length;

            this.chineseCharSet = this.dbContext.DbSet<ChineseChar>();
            this.failCharSet = this.dbContext.DbSet<FailChar>();
        }

        public Task Run()
        {
            IAggregateFluent<FailChar> query = null;
            if (failCode.HasValue)
                query = failCharSet.Where(w => w.FailCode == failCode.Value);
            else
                query = failCharSet.Aggregate();

            return query.ForEachAsync(async f =>
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.MaxResponseContentBufferSize = 256000;
                        httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/36.0.1985.143 Safari/537.36");
                        ChineseChar chineseChar = await CrawlerWorkItem.GetOneChinese(httpClient, uri, f.Unicode, len1, len2);
                        if(chineseChar==null)
                        {
                            CrawlerWorkItem.PrintFail(0, f.Unicode, f.Text);
                            CrawlerWorkItem.AddFailChar(failCharSet, 0, f.Unicode, f.Text, "");
                            return;
                        }

                        if (chineseCharSet.Any(a => a.Unicode == f.Unicode) == false)
                        {
                            chineseCharSet.Add(chineseChar);
                        }
                        failCharSet.Remove(f.Id);
                        Console.WriteLine($"已成功修复 Unicode：{f.Unicode} 内容：{f.Text}");
                    }
                }
                catch(Exception ex)
                {
                    CrawlerWorkItem.PrintFail(1, f.Unicode, f.Text);
                    CrawlerWorkItem.AddFailChar(failCharSet, 1, f.Unicode, f.Text, ex.Message);
                }
                finally
                {
                    await Task.Delay(200);
                }
            }).ContinueWith(t=>
            {
                var foregroundColor = ConsoleColor.White;
                if (t.IsCompletedSuccessfully == false)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(t.Exception.ToString());
                    Console.ForegroundColor = foregroundColor;
                }
     
                t.Dispose();
            });
        }

    }
}
