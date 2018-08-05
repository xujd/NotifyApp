using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialCommunication.Models
{
    public class MsgData
    {
        public string Data { get; set; }
        public int RetryNum { get; set; }
        public bool IsNeedAck { get; set; }

        public MsgData()
        {
            Data = "";
            RetryNum = 0;
            IsNeedAck = true;
        }
    }
}
