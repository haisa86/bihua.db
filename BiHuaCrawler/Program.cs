using MongoDB.EF;
using MongoDB.Driver;
using System;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using BiHuaCrawler.Model;

namespace BiHuaCrawler
{
    class Program
    {
        static MongoDBContext _dbContext = new MongoDBContext("mongodb://127.0.0.1:4444/bihua/?readPreference=SecondaryPreferred");
        static string url = "http://xue.hahaertong.com/index.php?app=chinese";

        static void Main(string[] args)
        {

            string i = Convert.ToString(10, 16);

            Console.ReadKey();


            //建立索引
            if (_dbContext.DbSet<ChineseChar>().Any() == false)
            {
                _dbContext.DbSet<ChineseChar>().Indexes.DropAll();
                var indexKeys = Builders<ChineseChar>.IndexKeys;
                var indexs = indexKeys.Ascending(x => x.Unicode).Ascending(x => x.Text);
                _dbContext.DbSet<ChineseChar>().Indexes.CreateOneAsync(new CreateIndexModel<ChineseChar>(indexs));
            }

            RunCrawler();
            Repair();

            _dbContext.DbSet<FailChar>().Aggregate().Group(k => k.FailCode, v => new
            {
                FailCode = v.Key,
                ErrorCount = v.LongCount(),
            }).ForEachAsync(f =>
            {
                var foregroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"FaileCode:{f.FailCode} 总数为：{f.ErrorCount}，详情请查看mongodb中的{nameof(FailChar)}集合");
                Console.ForegroundColor = foregroundColor;
            });

            Console.ReadKey();
        }

        /// <summary>
        /// 修复爬虫失败的数据
        /// </summary>
        static void Repair()
        {
            Console.WriteLine("开始尝试修复失败的字符!!!");
            new FailWorkItem(_dbContext, url, 1).Run().Wait();
            Console.WriteLine("修复完成!!!");
        }

        /// <summary>
        /// 开始爬虫
        /// </summary>
        static void RunCrawler()
        {
            Console.WriteLine("开始爬虫!!!");
            //爬取0x4E00~0x9FA5的字符
            ushort beginUnicode = 0x4E00;
            ushort endUnicode = 0x9FA5;

            //表示启动多少个线程进行数据爬取
            int count = 15;

            int regionCount = ((endUnicode - beginUnicode) + 1) / count;
            int mod = ((endUnicode - beginUnicode) + 1) % regionCount;

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < count; i++)
            {
                tasks.Add(new CrawlerWorkItem(
                    _dbContext, url,
                    (ushort)(beginUnicode + i * regionCount),
                    (ushort)(beginUnicode + (i + 1) * regionCount - 1))
                    .GetTask());
            }
            if (mod > 0)
            {
                tasks.Add(new CrawlerWorkItem(
                    _dbContext, url,
                    (ushort)(beginUnicode + count * regionCount),
                    (ushort)(beginUnicode + count * regionCount + mod - 1))
                    .GetTask());
            }
            Task.WhenAll(tasks).Wait();
            Console.WriteLine("爬虫运行完毕!!!");

        }


    }
}
