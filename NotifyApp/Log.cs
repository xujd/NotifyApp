using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NotifyApp
{
    class Log
    {
        public static ListBox LogList = null;

        public delegate void CleanDelegate();
        public delegate void LogDelegate(string log);
        public static void Clean()
        {
            //lock (Param.LOCK)
            //{
            //    if (Param.IsClosing || LogList == null) return;

            //    if (LogList.InvokeRequired && !Param.IsClosing)
            //    {
            //        LogList.Invoke(new CleanDelegate(() =>
            //        {
            //            if (!Param.IsClosing)
            //                LogList.Items.Clear();
            //        }));
            //    }
            //    else
            //    {
            //        if (!Param.IsClosing)
            //            LogList.Items.Clear();
            //    }
            //}
        }

        public static void WriteLine(string message, DateTime? dt = null)
        {
            lock (Param.LOCK)
            {
                if (Param.IsClosing || LogList == null) return;

                if (dt == null)
                    dt = DateTime.Now;
                if (LogList.InvokeRequired && !Param.IsClosing)
                {
                    LogList.Invoke(new LogDelegate((msg) =>
                    {
                        if (!Param.IsClosing)
                        {
                            LogList.Items.Insert(0, string.Format("{0}——{1}", dt, message));
                            if (LogList.Items.Count > 200)
                            {
                                LogList.Items.RemoveAt(LogList.Items.Count - 1);
                            }
                        }
                    }), string.Format("{0}——{1}", dt, message));
                }
                else
                {
                    if (!Param.IsClosing)
                    {
                        LogList.Items.Insert(0, string.Format("{0}——{1}", dt, message));
                        if (LogList.Items.Count > 200)
                        {
                            LogList.Items.RemoveAt(LogList.Items.Count - 1);
                        }
                    }
                }
            }
        }

        public static void WriteLog(string txt, bool flag = false)
        {
            try
            {
                string path = Application.StartupPath + @"\log\" + DateTime.Now.ToString("yyyy-MM") + @"\";
                if(!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                path += DateTime.Now.ToString("yyyyMMdd") + ".txt";
                if(!File.Exists(path))
                {
                    File.Create(path);
                }

                FileStream fs;
                StreamWriter sw;
                fs = new FileStream(path, FileMode.Append);
                sw = new StreamWriter(fs, Encoding.Default);
                sw.Write(DateTime.Now.ToString("HH:mm:ss") + " " + txt + "\r\n");
                sw.Close();
                fs.Close();
            }
            catch( Exception e)
            {
                if (!flag)
                    WriteLog("程序发生异常（WriteLog）。详情：" + e.Message, true);
            }
        }
    }
}
