using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace 包装队列显示
{
    class Cdevice
    {
        public string Dvid;
        public Socket Skt;
        public IPAddress Ip;
        public bool ReceiveFlg;

        public string Cmdqty;   //qty
        public string Cmderr;   //err state

        public bool leftFlg = false;
        public bool rightFlg = false;

        public string strLabel;   //条码打印指令

        public Cdevice() { }

        public Cdevice(string Dvid, Socket Skt, IPAddress Ip, bool ReceiveFlg, string Cmdqty, string Cmderr)
        {
            this.Dvid = Dvid;
            this.Skt = Skt;
            this.Ip = Ip;
            this.ReceiveFlg = ReceiveFlg;
            this.Cmdqty = Cmdqty;
            this.Cmderr = Cmderr;
        }
    }
    class ThreadCdevice
    {
        public Thread thread;
        public Cdevice cdevice;
        public ThreadCdevice(Thread thread, Cdevice cdevice)
        {
            this.thread = thread;
            this.cdevice = cdevice;
        }
    }
}
