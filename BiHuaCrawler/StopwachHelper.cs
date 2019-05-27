using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BiHuaCrawler
{
    public static class StopWachHelper
    {
        /// <summary>
        /// 执行计时
        /// </summary>
        /// <param name="action"></param>
        /// <param name="end"></param>
        public static void DoTimer(Action action,Action<TimeSpan> end)
        {
#if DEBUG
            Stopwatch sw = null;
            if (end != null)
            {
                sw = new Stopwatch();
                sw.Start();
            }
#endif
            action();
#if DEBUG
            sw?.Stop();
            end?.Invoke(sw.Elapsed);
#endif
        }
    }
}
