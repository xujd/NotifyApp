using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NotifyApp
{
    public class HostConfig
    {
        static char[] spliter = new char[] { ',', '，' };
        public static HostConfig Create(string[] configs)
        {
            HostConfig config = new HostConfig();
            
            foreach (var item in configs)
            {
                var kv = item.Split('=');
                if (kv.Length != 2) continue;

                if (kv[0] == "HOST")
                    config.Host = kv[1];
                else if (kv[0] == "USER")
                    config.User = kv[1];
                else if (kv[0] == "PWD")
                    config.Password = kv[1];
                else if (kv[0] == "PATH")
                    config.Path = kv[1];
                else if (kv[0] == "FILE")
                    config.FilePrefix = kv[1].Split(spliter).ToList();
                else if (kv[0] == "TYPE")
                    config.LineType = kv[1];
            }

            return config;
        }

        public string Host { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Path { get; set; }
        public List<string> FilePrefix { get; set; }
        public string LineType { get; set; }
        // string InOut { get; set; }

        public HostConfig()
        {
            LineType = "未知";
            //InOut = "未知";
        }
    }
}
