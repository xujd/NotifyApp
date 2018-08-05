using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace WpfInfoPusher.Models
{
    [Serializable]
    public class FtpTask : INotifyPropertyChanged
    {
        //任务类型：0-led显示，1-报文分析
        public int TaskType { get; set; }
        public long TaskId { get; set; }
        [XmlIgnore]
        public string TaskState
        {
            get { return taskState; }
            set
            {
                if (taskState != value)
                {
                    taskState = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("TaskState"));
                    }
                }
            }
        }
        private string taskState = "未开始";
        public string FilePath { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public string FtpUser { get; set; }
        public string FtpPassword { get; set; }
        public string TargetPath { get; set; }
        public string FtpFile { get; set; }
        [XmlIgnore]
        public DateTime LastestTime { get; set; }//最近一次的时间

        public event PropertyChangedEventHandler PropertyChanged;

        public FtpTask()
        {
            TaskType = 0;
            FtpFile = "";
            Port = "默认";
            FilePath = "";
            LastestTime = DateTime.Now;
        }
    }
}
