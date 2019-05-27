using MongoDB.EF;
using System;

namespace BiHuaCrawler
{
    /// <summary>
    /// 表示一个Unicode字符
    /// </summary>
    public class UnicodeChar : EntityBase
    {
        /// <summary>
        /// 获取或设置字符的unicode的值
        /// </summary>
        public ushort Unicode { get; set; }

        /// <summary>
        /// 获取或设置字符的字符串表示形式
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 获取或设置创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 获取或设置最后更改时间
        /// </summary>
        public DateTime ModifiedTime { get; set; }
    }
}
