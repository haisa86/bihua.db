using System;
using System.Collections.Generic;
using System.Text;

namespace BiHuaCrawler
{
    /// <summary>
    /// 爬虫失败的项
    /// </summary>
    public class FailChar: UnicodeChar
    {
        /// <summary>
        /// 失败代码
        /// 0：原网站返回结果为失败
        /// 1：爬虫程序出错
        /// </summary>
        public int FailCode { get; set; }

        /// <summary>
        /// 失败的提示信息
        /// </summary>
        public string FailMessage { get; set; }
    }
}
