using MongoDB.EF;
using System;
using System.Collections.Generic;
using System.Text;

namespace BiHuaCrawler.Model
{
    /// <summary>
    /// 用户记录当前爬虫单元
    /// </summary>
    public class CrawlerInfo: EntityBase
    {
        public ushort BeginUnicode { get; set; }

        public ushort EndUnicode { get; set; }

        /// <summary>
        /// 获取或设置当前已完成爬虫字符的unicode的值
        /// </summary>
        public ushort Unicode { get; set; }

        /// <summary>
        /// 是否已经完成
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 获取或设置最后更改时间
        /// </summary>
        public DateTime ModifiedTime { get; set; }
    }
}
