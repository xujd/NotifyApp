using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialCommunication.Models
{
    public class ComConfig
    {
        /// <summary>
        /// 车号文件路径
        /// </summary>
        public string FilePath { get; set; }
        /// <summary>
        /// 串口号
        /// </summary>
        public string SerialPort { get; set; }
        /// <summary>
        /// 比特率
        /// </summary>
        public string BitRate { get; set; }
        /// <summary>
        /// 数据位
        /// </summary>
        public string DataBit { get; set; }
        /// <summary>
        /// 停止位
        /// </summary>
        public string StopBit { get; set; }
        /// <summary>
        /// 校验
        /// </summary>
        public string Parity { get; set; }
        /// <summary>
        /// 通讯次数
        /// </summary>
        public int ComCount { get; set; }
        /// <summary>
        /// 再次发送延时
        /// </summary>
        public int TimeDelay { get; set; }

        public string TrainTypes { get; set; }
    }
}
