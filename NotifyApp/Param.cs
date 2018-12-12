using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace NotifyApp
{
    class Param
    {
        public static object LOCK = new object();

        public static bool IsClosing = false;
        public static int INTERVAL = 3;
        public static int IDLE_INTERVAL = 5;
        // 传感器文件来源
        public static string SRC_HOST = "";
        public static string SRC_USR = "";
        public static string SRC_PWD = "";
        public static string SRC_PATH = "";
        // 摄像头文件来源
        public static List<CHostConfig> CHostList = new List<CHostConfig>();

        public static List<HostConfig> HostList = new List<HostConfig>();

        public static List<DateTime> LastFileTime = new List<DateTime>();

        public static string FORMAT = "出入库签点牌  \r\n{0}股  {1}\r\n{2}\r\n{3} \r\n{4}";

        public static string DEFAULT = " 停车一分钟\r\n    记点";

        public static Dictionary<string, string> INOUT_MAP = new Dictionary<string, string>();

        public static void Init()
        {
            System.Configuration.AppSettingsReader asr = new System.Configuration.AppSettingsReader();

            FORMAT = ((string)asr.GetValue("format", typeof(string))).Replace("\\r\\n", "\r\n");
            DEFAULT = ((string)asr.GetValue("default", typeof(string))).Replace("\\r\\n", "\r\n");

            INTERVAL = (int)asr.GetValue("interval", typeof(int));
            IDLE_INTERVAL = (int)asr.GetValue("idle_interval", typeof(int));

            SRC_HOST = (string)asr.GetValue("src_host", typeof(string));
            SRC_USR = (string)asr.GetValue("src_usr", typeof(string));
            SRC_PWD = (string)asr.GetValue("src_pwd", typeof(string));
            SRC_PATH = (string)asr.GetValue("src_path", typeof(string));

            char[] spliter1 = new char[] { ';', '；' };
            char[] spliter2 = new char[] { ':', '：' };

            var inout_map = (string)asr.GetValue("inout_map", typeof(string));
            var temps = inout_map.Split(spliter1);
            foreach(var item in temps)
            {
                var strs = item.Split(spliter2);
                if (strs.Length != 2) continue;
                INOUT_MAP[strs[0]] = strs[1];
            }


            foreach (string key in ConfigurationManager.AppSettings)
            {
                if(key.StartsWith("dest_host"))
                {
                    var configs = ConfigurationManager.AppSettings[key].Split(spliter1);
                    var host = HostConfig.Create(configs, key);

                    HostList.Add(host);
                    LastFileTime.Add(DateTime.Now.AddMinutes(-5));
                }
            }

            foreach (string key in ConfigurationManager.AppSettings)
            {
                if (key.StartsWith("src_host_c"))
                {
                    var configs = ConfigurationManager.AppSettings[key].Split(spliter1);
                    var host = CHostConfig.Create(configs, key);

                    CHostList.Add(host);
                }
            }
        }

        public static int CheckFile(string fileName, string inout)
        {
            for (int i = 0; i < HostList.Count; i++)
            {
                foreach (var item in HostList[i].FilePrefix)
                {
                    if (item.Contains("@"))
                    {
                        var temps = item.Split('@');
                        if (fileName.StartsWith(temps[0]) && temps[1] == inout)
                            return i;
                    }
                    else if (fileName.StartsWith(item))
                        return i;
                }
            }

            return -1;
        }
    }
}
