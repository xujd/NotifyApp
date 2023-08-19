using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace SerialCommunication.Helpers
{
    class LogModel
    {
        public string Time { get; set; }
        public string Level { get; set; }
        public string Content { get; set; }

        public LogModel(string level, string content, string time)
        {
            this.Level = level;
            this.Content = content;
            this.Time = time;
        }
    }
    class Log
    {
        public static ObservableCollection<LogModel> LogList = null;

        public static Dispatcher dispatcher = null;

        static Log()
        {
            LogList = new ObservableCollection<LogModel>();
        }

        public delegate void CleanDelegate();
        public delegate void LogDelegate(string log);
        public static void Clean()
        {

        }

        public static void WriteLog(string level, string txt, string subdir = @"\log\", bool flag = false)
        {
            try
            {
                string path = Param.APPFILEPATH + subdir + DateTime.Now.ToString("yyyy-MM") + @"\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                path += DateTime.Now.ToString("yyyyMMdd") + ".txt";
                if (!File.Exists(path))
                {
                    File.Create(path);
                }


                var time = DateTime.Now.ToString("HH:mm:ss");
                string log = string.Format("{0} {1}：{2}", time, level, txt);

                FileStream fs;
                StreamWriter sw;
                fs = new FileStream(path, FileMode.Append);
                sw = new StreamWriter(fs, Encoding.Default);
                sw.Write(log + "\r\n");
                sw.Close();
                sw.Dispose();
                fs.Close();
                fs.Dispose();

                Log.dispatcher.Invoke(new Action(() =>
                {
                    LogList.Insert(0, new LogModel(level, txt, time));
                    if (LogList.Count > 150)//保留最新100条记录
                    {
                        while (LogList.Count > 100)
                        {
                            LogList.RemoveAt(LogList.Count - 1);
                        }
                    }
                }));

                //Dispatcher.CurrentDispatcher.BeginInvoke(Log.InvokeToList());
            }
            catch (Exception e)
            {
                if (!flag)
                {
                    WriteLog("ERR", "程序发生异常（WriteLog）。详情：" + e.Message, @"\log\", true);
                }
            }
        }

        private static void InvokeToList()
        {
            if (LogList.Count > 150)//保留最新100条记录
            {
                while (LogList.Count > 100)
                {
                    LogList.RemoveAt(LogList.Count - 1);
                }
            }
        }
    }
}
