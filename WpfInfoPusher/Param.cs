using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfInfoPusher
{
    class Param
    {
        public static string APPFILEPATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\xData\DataSwitching\";

        public static string FORMAT = "出入库签点牌  \r\n{0}股  {1}\r\n{2}\r\n{3} \r\n{4}";

        public static string MSGFORMAT = "1@0@{0}@{1}@{2}@1_{3}_{4}_0224";

        public static void Init()
        {
            System.Configuration.AppSettingsReader asr = new System.Configuration.AppSettingsReader();

            FORMAT = ((string)asr.GetValue("format", typeof(string))).Replace("\\r\\n", "\r\n");

            MSGFORMAT= ((string)asr.GetValue("msgformat", typeof(string)));
        }
    }
}
