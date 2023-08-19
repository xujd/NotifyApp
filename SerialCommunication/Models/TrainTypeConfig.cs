using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialCommunication.Models
{
    public class TrainTypeConfig
    {
        public string TrainType { get; set; }
        public int AddressNum { get; set; }
        public int[] Port { get; set; }
        public string TrainNo { get; set; }
    }
}
