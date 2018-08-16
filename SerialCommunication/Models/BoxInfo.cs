using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialCommunication.Models
{
    public class BoxInfo
    {
        // 序号
        public int No { get; set; }
        // 位置
        public int Position { get; set; }
        // 高度
        public int Height { get; set; }
        // 前伸
        public int FrontDist { get; set; }

        public BoxInfo()
        {
            No = 0;
            Position = 0;
            Height = 0;
            FrontDist = 0;
        }
    }

    public class TrainBox
    {
        // 车型
        public string TrainType { get; set; }
        // 车号
        public string TrainNo { get; set; }
        // 沙箱
        public List<BoxInfo> BoxList { get; set; }

        public TrainBox()
        {
            BoxList = new List<BoxInfo>();
        }
    }
}
