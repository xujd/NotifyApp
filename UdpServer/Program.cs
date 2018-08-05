using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UdpServer
{
    class Program
    {
        static UdpClient uc = null; //声明UDPClient
        static void Main(string[] args)
        { 
            uc = new UdpClient(8888);
            ////开线程
            //Thread th = new Thread(new ThreadStart(listen));
            ////设置为后台
            //th.IsBackground = true;
            //th.Start();

            listen();
        }

        static void listen()
        {
            //声明终结点
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            while (true)
            {
                //获得数据包
                string text = System.Text.Encoding.UTF8.GetString(uc.Receive(ref iep));

                System.Diagnostics.Debug.WriteLine(text);
                Console.WriteLine(text);
            }
        }

    }
}
