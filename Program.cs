using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace 包装后段测试
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
    class Program
    {
        static Cdevice[] Cdeviceobj = new Cdevice[]
        {

            new Cdevice("scanner_getweight",null,IPAddress.Parse("10.186.242.14"),false,"OK\r\n","NG\r\n")                                        //后段称重Scanner
            ,
            new Cdevice("robot_sealing",null,IPAddress.Parse("10.186.242.18"),false,"1,0,1,0,0,0,0,0,0,0,0,0,0,CR", "1,0,1,0,0,0,0,0,0,0,0,0,0,CR")    //贴标robot
            ,
            new Cdevice("scanner_athead",null,IPAddress.Parse("10.186.242.83"),false,"OK\r\n","NG\r\n")                                          //前段卡夹打单Scanner
            ,           
            new Cdevice("robot_catonlabel",null,IPAddress.Parse("10.186.242.88"),false,"1,CR","2,CR")                                              //贴标robot
            ,
            new Cdevice("scanner_afterlabeling",null,IPAddress.Parse("10.186.242.79"),false,"OK\r\n","NG\r\n")                                              //贴标后扫描校验
            ,
            new Cdevice("printer",null,IPAddress.Parse("10.186.242.80"),false,"OK\r\n","NG\r\n")                                              //打印机            
        };

        const int BURFERSIZE = 128;     //接受的数据缓冲区大小
        static int cnt = 0;

        static string mtsn = "";

        static void Main(string[] args)
        {
            Thread[] threads = new Thread[Cdeviceobj.Length];
            for (int i = 0; i < Cdeviceobj.Length; i++)
            {
                threads[i] = new Thread(ListenDevice);           //网络通信接收
                threads[i].IsBackground = true;
                threads[i].Start(Cdeviceobj[i]);
                Thread.Sleep(1000);

                ThreadPool.QueueUserWorkItem(new WaitCallback(PingDevice), new ThreadCdevice(threads[i], Cdeviceobj[i]));    //开启设备监听线程池,在多线程各自独立线程扫描心跳，响应及时

                if (Cdeviceobj[i].Dvid.Equals("scanner_athead"))           //前段卡夹打单开启  
                {
                    ListReload();

                    // RS232_setup
                    string com = "COM7";
                    mySerialPort = new SerialPort(com);
                    mySerialPort.BaudRate = 9600;
                    mySerialPort.DataBits = 8;
                    mySerialPort.Parity = Parity.None;
                    mySerialPort.StopBits = StopBits.One;
                    mySerialPort.Handshake = Handshake.None;
                    mySerialPort.RtsEnable = true;

                    try
                    {//
                    }
                    catch (Exception err)
                    {
                        File.AppendAllText("sysmsg.log", "\r\n " + DateTime.Now.ToString() + err.ToString(), Encoding.Default);
                    }

                    //清线模式清理队列
                    //UapdateMySqlteauto("autoscanlabel", "CLR1");
                    UapdateMySqlteauto("autoscanlabel", "CLR2");
                    UapdateMySqlteauto("autoscanlabel", "CLR3");
                    UapdateMySqlteauto("autoscanlabel", "CLR4");
                    UapdateMySqlteauto("autoscanlabel", "CLR5");
                    UapdateMySqlteauto("autoscanlabel", "CLR6");

                    //while ((processID = Autoit.AU3_WinExists("Pro-Make系统", "")) != 1)
                    if ((processID = Autoit.AU3_WinExists("Pro-Make系统", "")) != 1)
                    {
                        //Console.WriteLine("请先打开PROM客户端!...."); Thread.Sleep(2000);
                    }

                    Thread.Sleep(2000);
                    int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                    int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
                    Autoit.AU3_WinMove(@"AtuoPack", "", 0, 0, screenWidth / 3, screenHeight * 7 / 9);
                    Autoit.AU3_WinMove(@"C:\PTLabel\AtuoPack", "", 0, 0, screenWidth / 3, screenHeight * 7 / 9);
                    Autoit.AU3_WinMove(@"包装队列信息", "", 0, screenHeight * 7 / 9, screenWidth / 3, screenHeight * 2 / 9);

                    //Autoit.AU3_WinMove(@"GWSLINK", "", screenWidth * 2 / 3, 0, screenWidth * 1 / 3, screenHeight * 1 / 11);
                    //Autoit.AU3_WinMove(@"Pro-Make系统", "", screenWidth / 3, screenHeight / 9, screenWidth * 2 / 3 + 5, screenHeight * 7 / 11);
                    Autoit.AU3_WinMove(@"GWSLINK", "", screenWidth * 2 / 3, 0, screenWidth * 1 / 3, screenHeight * 1 / 11);
                    Autoit.AU3_WinMove(@"Pro-Make系统", "", screenWidth / 3, 0, screenWidth * 2 / 3 + 5, screenHeight * 7 / 11);

                    Autoit.AU3_WinClose(@"GWSLINK", "");
                    Autoit.AU3_WinActivate("Pro-Make系统", "");       ///使之窗口激活状态

                }
            }

            Myserialdevice();             //打开串口51控制器

            int timesleep = 15000;
            bool sleepflg = false;        //线体休眠标记
            while (true)
            {
                //主线程循环
                try
                {
                    Thread.Sleep(timesleep);

                    if (File.Exists("PowerSave"))
                    {
                        timesleep = Convert.ToInt32(File.ReadAllText("PowerSave").Trim());

                        int res = selectqueue_once();
                        if (1 == res)            //前段队列无数据
                        {
                            res = Stereolib_queue();
                            if (1 == res)              //立库附件盒已经清线
                            {
                                if (sktstorgescan != null)
                                {
                                    sktstorgescan.Skt.Send(Encoding.UTF8.GetBytes(sktstorgescan.Cmderr));   //ND指令 Pause PLC
                                }
                                else if (mySerialPort2 != null && mySerialPort2.IsOpen && !sleepflg)
                                {
                                    mySerialPort2.Write("ND\r\n");         //ND指令 Pause PLC
                                    sleepflg = true;
                                }
                            }
                        }
                        else
                        {
                            if (sktstorgescan != null)
                            {
                                sktstorgescan.Skt.Send(Encoding.UTF8.GetBytes(sktstorgescan.Cmdqty));   //OK指令  Pass
                            }
                            else if (mySerialPort2 != null && mySerialPort2.IsOpen && sleepflg)
                            {
                                mySerialPort2.Write("OK\r\n");            //OK指令  Pass
                                sleepflg = false;
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                }
            }
        }

        private static void ListenDevice(object obj)       //设备监听委托线程
        {
            Cdevice cdevice = (Cdevice)obj;
            int port = 8089;
            if (cdevice.Dvid.Equals("printer"))
            {
                port = 9100;
            }

            #region 网络断线自动重连接模式
            while (true)
            {
                cdevice.Skt = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建一个Socket
                Console.WriteLine("Connect " + cdevice.Dvid + " Info>>Conneting..." + DateTime.Now.ToString());

                try
                {
                    #region 异步网络socket方式
                    IAsyncResult connResult = cdevice.Skt.BeginConnect(cdevice.Ip, port, null, null);
                    connResult.AsyncWaitHandle.WaitOne(2000, true);  //等待2秒
                    //Console.WriteLine(connResult.IsCompleted);
                    if (!connResult.IsCompleted)        //网络ip不可达
                    {
                        Console.WriteLine(cdevice.Dvid + " 网络IP:" + cdevice.Ip + " 连接不可到达!:" + DateTime.Now.ToString());
                        Thread.Sleep(300);
                        continue;
                    }
                    else                //ip在线
                    {
                        if (!cdevice.Skt.Connected)      //socket连接失败
                        {
                            Console.WriteLine(cdevice.Dvid + " 网络IP:" + cdevice.Ip + " Socket连接失败!:" + DateTime.Now.ToString());
                            Thread.Sleep(300);
                            continue;
                        }
                        else            //socket连接成功
                        {
                            Console.WriteLine(cdevice.Dvid + " 网络IP:" + cdevice.Ip + " Network Socket Connect Sucess!:" + DateTime.Now.ToString());     //网络连接成功
                            if (cdevice.Dvid.Equals("robot_catonlabel"))
                            {
                                skttiebiao = cdevice;
                            }
                            else if (cdevice.Dvid.Equals("scanner_afterlabeling"))
                            {
                                skttiebiaoscan = cdevice;
                                dtPause = DateTime.Now;
                            }
                            else if (cdevice.Dvid.Equals("TEST040"))
                            {
                                sktzebra = cdevice;
                            }
                            else if (cdevice.Dvid.Equals("printer"))
                            {
                                sktzebranet = cdevice;
                            }
                            else if (cdevice.Dvid.Equals("TEST042"))
                            {
                                sktstorgescan = cdevice;
                            }
                        }
                    }
                    #endregion

                    while (true)
                    {
                        try
                        {
                            int length = 0;    //接受的数据长度
                            byte[] byteRecive = new byte[BURFERSIZE];     //接受的数据缓冲区大小
                            if ((length = cdevice.Skt.Receive(byteRecive, byteRecive.Length, SocketFlags.None)) > 0)//从服务器端接受返回信息
                            {
                                string strRecive = Encoding.UTF8.GetString(byteRecive, 0, length);      //字节数组接收转换到字符串                                
                                #region 贴检封机器手Robot
                                if (cdevice.Dvid == "robot_sealing")
                                {
                                    if (strRecive.Equals("START"))
                                    {
                                        Console.WriteLine("机器人请求发送工作指令!" + DateTime.Now.ToString());

                                        #region 自动切换模式
                                        if (File.Exists(@"robotcmd.ini"))
                                        {
                                            cdevice.Skt.Send(Encoding.UTF8.GetBytes(File.ReadAllText(@"robotcmd.ini", Encoding.Default)));
                                        }
                                        else
                                        {
                                            if (cdevice.ReceiveFlg)
                                            {
                                                //cdevice.Skt.Send(Encoding.UTF8.GetBytes(cdevice.Cmdqty));
                                                cdevice.Skt.Send(Encoding.UTF8.GetBytes(File.ReadAllText("robotcmd1.ini", Encoding.Default)));
                                            }
                                            else
                                            {
                                                cdevice.Skt.Send(Encoding.UTF8.GetBytes(File.ReadAllText("robotcmd2.ini", Encoding.Default)));
                                            }
                                            cdevice.ReceiveFlg = !cdevice.ReceiveFlg;
                                        }
                                        #endregion

                                        Console.WriteLine("上位机发送指令完成!" + DateTime.Now.ToString());
                                    }
                                    else if (strRecive.Equals("RECEIVED"))
                                    {
                                        Console.WriteLine("机器人收到指令回复应答!" + DateTime.Now.ToString());
                                    }
                                    else if (strRecive.Equals("CH"))   //暂不采用
                                    {
                                        Console.WriteLine("机器人反馈切换标签!" + DateTime.Now.ToString());
                                        string str = "";
                                        if (File.Exists(@"robotcmd.ini"))
                                        {
                                            str = File.ReadAllText(@"robotcmd.ini", Encoding.Default);
                                            if (str == "1,0,1,0,0,0,0,0,0,0,0,0,0,CR")
                                            {
                                                File.WriteAllText(@"robotcmd.ini", "2,0,1,0,0,0,0,0,0,0,0,0,0,CR", Encoding.Default);
                                            }
                                            else if (str == "2,0,1,0,0,0,0,0,0,0,0,0,0,CR")
                                            {
                                                File.WriteAllText(@"robotcmd.ini", "1,0,1,0,0,0,0,0,0,0,0,0,0,CR", Encoding.Default);
                                            }
                                        }
                                        else
                                        {
                                            File.WriteAllText(@"robotcmd.ini", "1,0,1,0,0,0,0,0,0,0,0,0,0,CR", Encoding.Default);   //随机一次
                                        }
                                    }
                                    else if (strRecive.Equals("CH1"))
                                    {
                                        Console.WriteLine("机器人反馈切换到1号标签!" + DateTime.Now.ToString());
                                        File.AppendAllText("change.log", "\r\n 机器人反馈切换到1号标签!" + DateTime.Now.ToString(), Encoding.Default);
                                        File.WriteAllText(@"robotcmd.ini", "1,0,1,0,0,0,0,0,0,0,0,0,0,CR", Encoding.Default);
                                    }
                                    else if (strRecive.Equals("CH2"))
                                    {
                                        Console.WriteLine("机器人反馈切换到2号标签!" + DateTime.Now.ToString());
                                        File.AppendAllText("change.log", "\r\n 机器人反馈切换到2号标签!" + DateTime.Now.ToString(), Encoding.Default);
                                        File.WriteAllText(@"robotcmd.ini", "2,0,1,0,0,0,0,0,0,0,0,0,0,CR", Encoding.Default);
                                    }
                                    else if (strRecive.Equals("ED"))        //可以不用
                                    {
                                        Console.WriteLine("机器人反馈动作完成!" + DateTime.Now.ToString());
                                    }
                                }
                                #endregion

                                #region 扫描称重一体机
                                else if (cdevice.Dvid.Equals("scanner_getweight"))   //Scanner
                                {
                                    if (strRecive.Length > 1)
                                    {
                                        if (strRecive.Substring(0, 2).Equals("1S"))    //读到条码
                                        {
                                            mtsn = strRecive.Substring(0, 20);  //mtsn = strRecive; //里面包含回车符
                                            Console.WriteLine("扫描的条码信息:" + strRecive + DateTime.Now.ToString());
                                            cdevice.Skt.Send(new byte[] { 0X02, 0X41, 0X43, 0X30, 0X32, 0X03 });  //询问重量
                                        }
                                        else if (strRecive.Substring(0, 6).Equals("NOREAD"))  //没读到条码
                                        {
                                            cdevice.Skt.Send(Encoding.UTF8.GetBytes(cdevice.Cmderr));
                                            Console.WriteLine("NG:" + "NOREAD          ");
                                            cdevice.Skt.Send(Encoding.UTF8.GetBytes("NG:" + "NOREAD          \r\n"));
                                            File.AppendAllText("WeightData.txt", "\r\n机器流水号:" + "NG:" + "NOREAD          " + " 记录时间:" + DateTime.Now.ToString(), Encoding.Default);
                                        }
                                        else if (strRecive.Substring(0, 2).Equals("\u0002A"))
                                        {
                                            string strweight = strRecive.Substring(3, 8);
                                            Console.WriteLine("返回的重量信息:" + strRecive + "\n" + DateTime.Now.ToString());
                                            int weight = Convert.ToInt32(strweight);

                                            if (strweight.Substring(0, 1).Equals("+"))
                                            {
                                                if (weight < 1500)
                                                {
                                                    Console.WriteLine("NG:" + "UNDERWEIGHT");
                                                    File.AppendAllText("WeightData.txt", "\r\n机器流水号:" + mtsn + " 缺重!!!  实际重量:，" + weight + "， 记录时间:" + DateTime.Now.ToString(), Encoding.Default);
                                                }
                                                else if (weight > 3500)
                                                {
                                                    Console.WriteLine("NG:" + "OVERWEIGHT ");
                                                    File.AppendAllText("WeightData.txt", "\r\n机器流水号:" + mtsn + " 超重!!!  实际重量:，" + weight + "， 记录时间:" + DateTime.Now.ToString(), Encoding.Default);
                                                }
                                                else
                                                {
                                                    Console.WriteLine("OK");
                                                    cdevice.Skt.Send(Encoding.UTF8.GetBytes(cdevice.Cmdqty));   //OK指令  pass
                                                    //cdevice.Skt.Send(Encoding.UTF8.GetBytes(Chr(2) + " Uwip:"+ mtsn + " Weight:"+ weight+ "\r\n"));    //Skt.Send(Encoding.UTF8.GetBytes(Chr(2)+"xSAAA012345678912345678345BCEy"+"\r\n"));;//33个字符-3 = 30
                                                    cdevice.Skt.Send(Encoding.UTF8.GetBytes(Chr(2) + mtsn + " : " + weight + "(g) -> OK\r\n"));
                                                    File.AppendAllText("WeightData.txt", "\r\n机器流水号:" + mtsn + "  实际重量:，" + weight + "， 记录时间:" + DateTime.Now.ToString(), Encoding.Default);
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("重量数据不符合要求:" + strweight);
                                                cdevice.Skt.Send(Encoding.UTF8.GetBytes(cdevice.Cmderr));         //是否屏蔽停机
                                                File.AppendAllText("WeightData.txt", "\r\n机器流水号:" + mtsn + "  重量不符! " + " 记录时间:" + DateTime.Now.ToString(), Encoding.Default);
                                            }
                                            Console.WriteLine("实际重量:" + weight);
                                            InsertMySqlteauto("autoscanweight", mtsn, weight);
                                            mtsn = "";
                                        }
                                    }
                                }
                                #endregion

                                #region 扫描机器卡夹打单一体机
                                else if (cdevice.Dvid.Equals("scanner_athead"))   //Scanner
                                {
                                    string thismtsn = strRecive.Substring(0, strRecive.Length - 2);
                                    if (bakmtsn.Equals(thismtsn))
                                    {
                                        cdevice.Skt.Send(Encoding.UTF8.GetBytes(cdevice.Cmderr));  //NG指令  NOREAD or SN重复等未知情况停机 OUT2
                                    }
                                    else
                                    {
                                        bakmtsn = thismtsn;

                                        DateTime starttime = DateTime.Now;

                                        //ProcessScandataDisk(strRecive, cdevice);

                                        #region 数据库为主，盘片为辅，输出三种状态，和相关卡夹和贴单等信息
                                        string mtsnfront, brandname; Result_back.LabelNumber checkFlg;
                                        Result_back.LabelResult resprocess = number_labeltopaste(strRecive, out mtsnfront, out brandname, out checkFlg);
                                        //if (resprocess == 1)
                                        if (resprocess != Result_back.LabelResult.noread_others)   //1 or 3
                                        {
                                            if (queue_firstinsert("autoscanlabel", mtsnfront, brandname, (int)checkFlg))      //写入数据库要判断返回成功  
                                            {
                                                cdevice.Skt.Send(Encoding.UTF8.GetBytes(cdevice.Cmdqty));   //OK指令  Pass   OUT1
                                                if (checkFlg > 0 && checkFlg < 10)                                        //说明要装卡夹和纸箱贴标
                                                {
                                                    if (verticalbank_insert(mtsnfront))
                                                    {
                                                        Console.WriteLine("立库存储过程执行成功!");
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("立库存储过程执行失败!");
                                                        File.AppendAllText("InsertMySqlonline.log", "\r\n 立库存储过程执行失败!");  //写入日志
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                cdevice.Skt.Send(Encoding.UTF8.GetBytes("ND\r\n"));         //ND指令  fail处理, OUT3
                                            }
                                        }
                                        else if (resprocess == Result_back.LabelResult.noread_others)
                                        {
                                            cdevice.Skt.Send(Encoding.UTF8.GetBytes(cdevice.Cmderr));       //NG指令  NOREAD or 未知情况停机 OUT2
                                        }
                                        else
                                        {
                                            cdevice.Skt.Send(Encoding.UTF8.GetBytes("ND\r\n"));             //ND指令  fail处理, OUT3
                                        }
                                        #endregion

                                        DateTime endtime = DateTime.Now;
                                        Console.WriteLine(thismtsn + "," + starttime.ToString() + "," + endtime.ToString() + ",扫描处理时间耗时" + (endtime - starttime).ToString());
                                        //File.AppendAllText("ScanData.csv", "\r\n" + thismtsn + "," + starttime.ToString() + "," + endtime.ToString() + "," + (endtime - starttime).ToString(), Encoding.Default);

                                        if (++cnt > 100)
                                        {
                                            ListReload();
                                            cnt = 0;
                                        }
                                    }
                                }
                                #endregion

                                #region 贴卡通标Robot
                                else if (cdevice.Dvid.Equals("robot_catonlabel"))
                                {
                                    cdevice.strLabel = strRecive;
                                    processLabel(cdevice);        //阻塞数据接收                                   
                                }
                                #endregion

                                #region 贴卡通标扫描校验
                                else if (cdevice.Dvid.Equals("scanner_afterlabeling"))         //242.79 scanner
                                {
                                    if (strRecive.Substring(0, 2).Equals("1S"))    //读到条码
                                    {

                                        string uwipchk = "$"; string Cartonchk = "$";
                                        if (autoscan_waitforprint(ref uwipchk, ref Cartonchk))
                                        {
                                            Console.WriteLine("找到DB中待校验条码!-" + uwipchk);
                                        }
                                        else
                                        {
                                            Console.WriteLine("没找到DB中待校验条码!-" + uwipchk);
                                            File.AppendAllText("PROMLBL.log", "\r\n" + "没找到DB中待校验条码!-" + uwipchk + "," + DateTime.Now.ToString(), Encoding.Default);
                                        }

                                        if (strRecive.Contains(uwipchk))    //读到流水号条码
                                        {
                                            cdevice.Skt.Send(Encoding.UTF8.GetBytes(cdevice.Cmdqty));   //OK指令  Pass
                                            Console.WriteLine(uwipchk + ",8,CR");
                                            //File.AppendAllText("PROMLBL.log", "\r\n" + "贴标扫描ED-PROM贴单扫描校验数据正确!，" + uwipchk + "," + DateTime.Now.ToString(), Encoding.Default);

                                            //判断键存在
                                            if (dicmtsnmo.ContainsKey(uwipchk)) // True 
                                            {
                                                Console.WriteLine("An element with Key = \"key1\" exists.");
                                                UapdateMySqlteauto("plan_data", "qty_chg_add", dicmtsnmo[uwipchk]);       //附件盒预加工系统订单计数
                                            }
                                        }
                                        else
                                        {
                                            cdevice.Skt.Send(Encoding.UTF8.GetBytes(cdevice.Cmderr));  //NG指令  Fail
                                            Console.WriteLine(uwipchk + ",7,CR");
                                            File.AppendAllText("PROMLBL.log", "\r\n" + "贴标扫描ED-PROM贴单扫描校验数据错误!，" + strRecive + "," + uwipchk + "," + DateTime.Now.ToString(), Encoding.Default);
                                        }

                                        if (UapdateMySqlteauto("autoscanlabel", "ED"))   //数据库出队列成功
                                        {
                                            if (strRecive.Contains(uwipchk))    //读到流水号条码 //是否给立体库指令或者入新队列
                                            {
                                                //UapdateMySqlteauto("autoscanlabel", "STG");   //数据库入立体库队列成功
                                                Console.WriteLine("贴标扫描ED-更新PROM贴单扫描校验完成时间DTimeFourth标记成功!入立体库队列成功!");    //扫描器的串口数据会发送SN给后面附件立体库
                                            }
                                            else
                                            {
                                                //UapdateMySqlteauto("autoscanlabel", "CANCEL");   //数据库取消入立体库队列
                                                Console.WriteLine("贴标扫描ED-更新PROM贴单扫描校验完成时间DTimeFourth标记成功!入立体库队列取消!!!");
                                            }
                                            //File.AppendAllText("PROMLBL.log", "\r\n" + "贴标扫描ED-更新PROM贴单扫描校验完成时间DTimeFourth标记成功!，" + DateTime.Now.ToString(), Encoding.Default);
                                        }
                                        else
                                        {
                                            Console.WriteLine("贴标扫描ED-更新PROM贴单扫描校验完成时间DTimeFourth标记失败!");
                                            File.AppendAllText("PROMLBL.log", "\r\n" + "贴标扫描ED-更新PROM贴单扫描校验完成时间DTimeFourth标记失败!，" + DateTime.Now.ToString(), Encoding.Default);
                                        }
                                    }
                                    else if (strRecive.Substring(0, 2).Equals("ON"))
                                    {
                                        if (sktzebra != null)
                                        {
                                            string ptcmd = "^XA~PP~HS^XZ";
                                            ptcmd = "~PP~HS";

                                            tsPause = DateTime.Now.Subtract(dtPause);    //dtpause到现在多长时间
                                            if (tsPause.TotalSeconds > 2)           //超过2s
                                            {
                                                dtPause = DateTime.Now;
                                                PrinterStatus = "PP";

                                                if (File.Exists("PP"))
                                                {
                                                    sktzebra.Skt.Send(Encoding.UTF8.GetBytes(ptcmd));
                                                }
                                                else if (File.Exists("PPN"))
                                                {
                                                    sktzebranet.Skt.Send(Encoding.UTF8.GetBytes(ptcmd));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //btnManualPrintCtl(gwszebrea, "PP");
                                        }

                                        Console.WriteLine("ON->Pause!");
                                    }
                                    else if (strRecive.Substring(0, 3).Equals("OFF"))
                                    {
                                        //Console.WriteLine("OFF");
                                    }
                                    else if (strRecive.Substring(0, 6).Equals("NOREAD"))  //没读到条码
                                    {
                                        cdevice.Skt.Send(Encoding.UTF8.GetBytes(cdevice.Cmderr));  //NG指令  Fail 停机处理

                                        string uwipchk = "$"; string Cartonchk = "$";
                                        if (autoscan_waitforprint(ref uwipchk, ref Cartonchk))
                                        {
                                            Console.WriteLine("找到DB中待校验条码!-" + uwipchk);
                                        }
                                        else
                                        {
                                            Console.WriteLine("没找到DB中待校验条码!-" + uwipchk);
                                            File.AppendAllText("PROMLBL.log", "\r\n" + "没找到DB中待校验条码!-" + uwipchk + "," + DateTime.Now.ToString(), Encoding.Default);
                                        }

                                        Console.WriteLine(uwipchk + ",7,CR");
                                        File.AppendAllText("PROMLBL.log", "\r\n" + "贴标扫描ED- PROM贴单扫描校验失败!-NOREAD，" + uwipchk + "," + DateTime.Now.ToString(), Encoding.Default);

                                        if (UapdateMySqlteauto("autoscanlabel", "ED"))
                                        {
                                            Console.WriteLine("贴标扫描ED-更新PROM贴单扫描校验完成时间DTimeFourth标记成功!");
                                            //File.AppendAllText("PROMLBL.log", "\r\n" + "贴标扫描ED-更新PROM贴单扫描校验完成时间DTimeFourth标记成功!，" + DateTime.Now.ToString(), Encoding.Default);
                                        }
                                        else
                                        {
                                            Console.WriteLine("贴标扫描ED-更新PROM贴单扫描校验完成时间DTimeFourth标记失败!");
                                            File.AppendAllText("PROMLBL.log", "\r\n" + "贴标扫描ED-更新PROM贴单扫描校验完成时间DTimeFourth标记失败!，" + DateTime.Now.ToString(), Encoding.Default);
                                        }
                                    }
                                }
                                #endregion

                                #region ZebraRS232
                                else if (cdevice.Dvid.Equals("TEST040"))        //242.70  串口IP
                                {
                                    if (strRecive.Contains("警报: 打印机暂停"))
                                    {
                                        if (!PrinterStatus.Equals("PP"))
                                        {
                                            Console.WriteLine("打印机暂停失败!!!");
                                            PrinterStatus = "PP";
                                        }
                                    }
                                    else if (strRecive.Contains("警报清除: 打印机暂停"))
                                    {
                                        if (!PrinterStatus.Equals("PS"))
                                        {
                                            Console.WriteLine("打印机暂停取消失败!!!");
                                            PrinterStatus = "PS";
                                        }
                                    }
                                    else if (strRecive.Contains("错误条件: 打印纸用完"))
                                    {
                                        Console.WriteLine("打印机缺纸报警!!!");
                                        skttiebiaoscan.Skt.Send(Encoding.UTF8.GetBytes("NB\r\n"));  //NB , 缺纸
                                        File.AppendAllText("Printer.log", strRecive, Encoding.Default);
                                    }
                                    else if (strRecive.Contains("错误已清除: 打印纸用完"))
                                    {
                                        Console.WriteLine("打印机更换纸OK!!!");
                                    }
                                    else if (strRecive.Contains("1234,"))
                                    {
                                        string Receive = strRecive;
                                        string cmdfeedbacklabel = Receive.Split('\n')[0].Split(',')[1];       //缺纸检查
                                        string cmdfeedbackstatus = Receive.Split('\n')[0].Split(',')[2];      //打印暂停检查

                                        if (cmdfeedbacklabel.Equals("1"))
                                        {
                                            Console.WriteLine("打印机缺纸!");
                                            File.AppendAllText("Printer.log", "\r\n" + "打印机缺纸!，" + DateTime.Now.ToString(), Encoding.Default);
                                            File.AppendAllText("printer.csv", "\r\n缺纸报警!," + DateTime.Now.ToString(), Encoding.Default);
                                            skttiebiaoscan.Skt.Send(Encoding.UTF8.GetBytes("NB\r\n"));  //NB , 缺纸
                                        }
                                        else if (cmdfeedbackstatus.Equals("1"))
                                        {
                                            Console.WriteLine("打印机暂停控制OK!");
                                            PrinterStatus = "PP";
                                            //File.AppendAllText("printer.csv", "\r\nP," + DateTime.Now.ToString(), Encoding.Default);
                                            File.AppendAllText("printer.csv", "\r\n" + PrinterStatus.Substring(1, 1) + "," + DateTime.Now.ToString(), Encoding.Default);
                                        }
                                        else
                                        {
                                            Console.WriteLine("打印机启动成功!");
                                            PrinterStatus = "PS";
                                            //File.AppendAllText("printer.csv", "\r\nS," + DateTime.Now.ToString(), Encoding.Default);
                                            File.AppendAllText("printer.csv", "\r\n" + PrinterStatus.Substring(1, 1) + "," + DateTime.Now.ToString(), Encoding.Default);
                                        }
                                    }
                                }
                                #endregion

                                #region ZebraNet
                                else if (cdevice.Dvid.Equals("printer"))
                                {
                                    if (strRecive.Contains("警报: 打印机暂停"))
                                    {
                                        if (!PrinterStatus.Equals("PP"))
                                        {
                                            Console.WriteLine("打印机暂停失败!!!");
                                            PrinterStatus = "PP";
                                        }
                                    }
                                    else if (strRecive.Contains("警报清除: 打印机暂停"))
                                    {
                                        if (!PrinterStatus.Equals("PS"))
                                        {
                                            Console.WriteLine("打印机暂停取消失败!!!");
                                            PrinterStatus = "PS";
                                        }
                                    }
                                    else if (strRecive.Contains("错误条件: 打印纸用完"))
                                    {
                                        Console.WriteLine("打印机缺纸报警!!!");
                                        skttiebiaoscan.Skt.Send(Encoding.UTF8.GetBytes("NB\r\n"));  //NB , 缺纸
                                        File.AppendAllText("Printer.log", strRecive, Encoding.Default);
                                    }
                                    else if (strRecive.Contains("错误已清除: 打印纸用完"))
                                    {
                                        Console.WriteLine("打印机更换纸OK!!!");
                                    }
                                    else if (strRecive.Contains("1234,"))
                                    {
                                        string Receive = strRecive;
                                        string cmdfeedbacklabel = Receive.Split('\n')[0].Split(',')[1];       //缺纸检查
                                        string cmdfeedbackstatus = Receive.Split('\n')[0].Split(',')[2];      //打印暂停检查

                                        if (cmdfeedbacklabel.Equals("1"))
                                        {
                                            Console.WriteLine("打印机缺纸!");
                                            File.AppendAllText("Printer.log", "\r\n" + "打印机缺纸!，" + DateTime.Now.ToString(), Encoding.Default);
                                            File.AppendAllText("printer.csv", "\r\n缺纸报警!," + DateTime.Now.ToString(), Encoding.Default);
                                            skttiebiaoscan.Skt.Send(Encoding.UTF8.GetBytes("NB\r\n"));  //NB , 缺纸
                                        }
                                        else if (cmdfeedbackstatus.Equals("1"))
                                        {
                                            Console.WriteLine("打印机暂停控制OK!");
                                            PrinterStatus = "PP";
                                            //File.AppendAllText("printer.csv", "\r\nP," + DateTime.Now.ToString(), Encoding.Default);
                                            File.AppendAllText("printer.csv", "\r\n" + PrinterStatus.Substring(1, 1) + "," + DateTime.Now.ToString(), Encoding.Default);
                                        }
                                        else
                                        {
                                            Console.WriteLine("打印机启动成功!");
                                            PrinterStatus = "PS";
                                            //File.AppendAllText("printer.csv", "\r\nS," + DateTime.Now.ToString(), Encoding.Default);
                                            File.AppendAllText("printer.csv", "\r\n" + PrinterStatus.Substring(1, 1) + "," + DateTime.Now.ToString(), Encoding.Default);
                                        }
                                    }
                                }
                                #endregion

                                #region 立库扫描
                                else if (cdevice.Dvid.Equals("TEST042"))
                                {
                                    File.AppendAllText("ScannerStorge.csv", "\r\n" + strRecive + "," + DateTime.Now.ToString(), Encoding.Default);
                                    if (strRecive.Substring(0, 2).Equals("1S"))    //读到条码
                                    {
                                    }
                                    else if (strRecive.Substring(0, 6).Equals("NOREAD"))  //没读到条码
                                    {
                                    }
                                }
                                #endregion
                            }
                            else   //接收长度0
                            {
                                Console.WriteLine("Server 断开!!!: " + cdevice.Dvid + " is Shutdown! ");
                                File.AppendAllText(cdevice.Dvid + ".log", "\r\n Server 断开!!!: " + cdevice.Dvid + " is Shutdown! " + DateTime.Now.ToString(), Encoding.Default);
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("网络接收数据出现异常! ");
                            File.AppendAllText(cdevice.Dvid + ".log", "\r\n 网络数据接收出现异常!!!: " + DateTime.Now.ToString(), Encoding.Default);
                            break;
                        }
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine("网络连接出现异常，请重启现场设备等待尝试自动重新连接，连接尝试进行中......");
                    File.AppendAllText(cdevice.Dvid + ".log", "\r\n 网络连接通讯异常!!!自动重新连接尝试进行中: " + DateTime.Now.ToString(), Encoding.Default);
                }
                finally
                {
                    try
                    {
                        if (cdevice.Skt != null)
                        {
                            cdevice.Skt.Close();
                            Thread.Sleep(3000);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("SocketException:{0}", e);
                    }
                }
            }
            #endregion
        }


        static void ListReload()
        {
            File.AppendAllText("ListReload.log", "\r\n" + "ListReload OK!" + DateTime.Now.ToString(), Encoding.Default);
            if (File.Exists("DataLog.txt"))
            {
                //File.Copy("DataLog.txt", mtsnbakpath + "DataLog.txt", true);    //backup to server
            }
            lstsiteOverpack = new List<string>();
            lstsiteTW = new List<string>();
            if (File.Exists(@"\\cml2s460\tables\site.ini"))
            {
                File.Copy(@"\\cml2s460\tables\site.ini", "site.ini", true);
                string[] strsitelines = File.ReadAllLines("site.ini");
                foreach (string line in strsitelines)
                {
                    if (line.Contains(":COOPRO"))           //overpack定义
                    {
                        lstsiteOverpack.Add(line.Split(':')[0]);
                    }
                    else if (line.Contains(":PARTPRO"))      //taiwan定义
                    {
                        lstsiteTW.Add(line.Split(':')[0]);
                    }
                }
            }

            lstmectl = new List<string>();
            if (File.Exists(@"\\cml2s460\tables\mectl.ini"))
            {
                File.Copy(@"\\cml2s460\tables\mectl.ini", "mectl.ini", true);
                string[] strlines = File.ReadAllLines("mectl.ini");
                foreach (string line in strlines)
                {
                    if (line.Contains(":"))
                    {
                        lstmectl.Add(line);
                    }
                }
            }
        }



        static string gwszebrea = "10.186.242.87";
        static string PrinterStatus = string.Empty;

        static string uwipbak;   //记录当前贴标的uwip是否正常及时移除队列
        static void processLabel(object obj)        //贴标处理
        {
            Cdevice cdevice = (Cdevice)obj;
            string strRecive = cdevice.strLabel;
            #region 贴标Robot
            if (strRecive.Equals("REQUEST"))
            {
                int retrytimes = 0;
            Retry:
                string uwip = "", materialnumber = "", cmdinfo = "", brandnameTrayType = "";
                bool processflg = false;
                if (label_checkforstate_andnumber(ref uwip, ref materialnumber, ref cmdinfo, ref brandnameTrayType))
                {
                    if (cmdinfo.Equals("ROBOTCANCEL"))  //MES报错
                    {
                        cdevice.Skt.Send(Encoding.UTF8.GetBytes("4" + ",CR"));   //让机器人直接不贴单就放行机器
                        UapdateMySqlteauto("autoscanlabel", "ED");
                        processflg = true;
                        Console.WriteLine("4,CR");
                    }
                    else if (cmdinfo.Equals("PRINT"))   //等待打单，重试retry尝试等待最多3次
                    {
                        processflg = false;
                        Console.WriteLine("PRINT" + ",CR");
                        File.AppendAllText("cmdinfo.log", "\r\n查询到PRINT标记，STR不能发送贴标指令!" + uwip + DateTime.Now.ToString(), Encoding.Default);
                    }
                    else        //正常打出单，可以贴单
                    {
                        #region Overpack只打印和粘贴第一张，后续取消打印
                        if (materialnumber == "5")
                        {
                            if (PrinterStatus.Equals("PP"))
                            {
                                Thread.Sleep(200);
                            }
                            else
                            {
                                while (!PrinterStatus.Equals("PP"))
                                {
                                    if (SelectMySqlteautoPPOK())
                                    {
                                        UapdateMySqlteautoPPOK("N");    //再复位更新成'N'
                                        break;
                                    }
                                    else if (File.Exists("PPOK"))
                                    {
                                        File.Delete("PPOK");
                                        break;
                                    }
                                    else
                                    {
                                        Thread.Sleep(2000);
                                        Console.WriteLine("打印机状态被干预！" + DateTime.Now.ToString() + "休息一会儿!再重新检测");
                                    }
                                }

                                Console.WriteLine("解除干预OK！" + DateTime.Now.ToString());
                                PrinterStatus = "PP";
                            }


                            if (PrinterStatus.Equals("PP"))
                            {
                                PrinterStatus = "JA"; //Thread.Sleep(2000);

                                if (sktzebra != null)
                                {
                                    sktzebra.Skt.Send(Encoding.UTF8.GetBytes("~JA"));           //如果机器手等待取标时询问贴标坐标动态变化值稳定的话，可以在不用此行
                                }
                                else
                                {
                                    btnManualPrintCtl(gwszebrea, "JA");
                                }

                                if (File.Exists("overpackspeed"))
                                {
                                    UapdateMySqlteauto("autoscanlabel", "6");     //如果overpack是则切换5->6      
                                }
                            }
                            else if (SelectMySqlteautoPPOK())
                            {
                                //UapdateMySqlteautoPPOK("N");    //再复位更新成'N'
                            }
                            else if (File.Exists("PPOK"))
                            {
                                //File.Delete("PPOK");
                            }

                            Console.WriteLine("JA ok");
                            File.AppendAllText("cmdinfo.log", "\r\n查询到OverPack标记，STR发送首张贴标指令和取消后续打印!" + uwip + DateTime.Now.ToString(), Encoding.Default);
                        }
                        #endregion

                        if (brandnameTrayType.Equals("T460s") || brandnameTrayType.Equals("T470s") || brandnameTrayType.Equals("T480s"))   //不带外置电池, 会直接贴标，否则会让机器人等待1s后贴标
                        {
                            if (materialnumber == "5" || materialnumber == "6")
                            {
                                cdevice.Skt.Send(Encoding.UTF8.GetBytes("11,CR"));    //只贴首张
                            }
                            else
                            {
                                cdevice.Skt.Send(Encoding.UTF8.GetBytes(materialnumber + "1,CR"));
                            }
                        }
                        else
                        {
                            if (materialnumber == "5" || materialnumber == "6")
                            {
                                cdevice.Skt.Send(Encoding.UTF8.GetBytes("1,CR"));    //只贴首张
                            }
                            else
                            {
                                cdevice.Skt.Send(Encoding.UTF8.GetBytes(materialnumber + ",CR"));
                            }
                        }

                        processflg = true;
                        Console.WriteLine(materialnumber + ",CR");
                    }
                }
                else
                {
                    File.AppendAllText("cmdinfo.log", "\r\n查询select失败无记录，STR不能发送贴标指令!" + DateTime.Now.ToString(), Encoding.Default);
                    Console.WriteLine("select失败");
                    processflg = false;
                }

                if (processflg)
                {
                    Console.WriteLine("发送机器人贴标指令成功!");
                }
                else
                {
                    Console.WriteLine("发送机器人贴标指令失败!Retry数:" + retrytimes + "次!" + DateTime.Now.ToString());
                    File.AppendAllText("cmdinfo.log", "\r\n发送机器人贴标指令失败!STR不能发送贴标指令!Retry数:" + retrytimes + "次," + DateTime.Now.ToString(), Encoding.Default);
                    if (++retrytimes <= 9)
                    {
                        Thread.Sleep(1000 * retrytimes);
                        goto Retry;
                    }
                    cdevice.Skt.Send(Encoding.UTF8.GetBytes("0" + ",CR"));   //让机器人什么都不做而回到起始位
                }

                uwipbak = uwip;             //备份当前贴标的机器
                Console.WriteLine("str ok");
            }
            else if (strRecive.Equals("RC"))  //没有用
            {
                Console.WriteLine("rc ok");
            }
            else if (strRecive.Equals("END"))
            {
                Console.WriteLine("end ok  " + DateTime.Now);

                #region 针对错位贴单进行软件防呆校验
                Thread.Sleep(2000);
                string uwip = "", materialnumber = "", cmdinfo = "", brandnameTrayType = "";
                int i = 0;
                while (true)
                {
                    if (label_checkforstate_andnumber(ref uwip, ref materialnumber, ref cmdinfo, ref brandnameTrayType))
                    {
                        if (uwip != "")
                        {
                            if (uwip != uwipbak)
                            {
                                break;
                            }
                            else  //同一台机器未及时出队列
                            {
                                File.AppendAllText("uwipbakmsg.log", "\r\n此台机器" + uwipbak + "未及时出队列!" + DateTime.Now.ToString(), Encoding.Default);
                            }
                        }
                    }
                    else
                    {
                        //File.AppendAllText("cmdinfo.log", "\r\n查询select失败无记录!" + DateTime.Now.ToString(), Encoding.Default);
                        Console.WriteLine("ED 后面没有打好单的机器,retry次数:" + i);
                    }

                    Thread.Sleep(1000);
                    if (++i > 10)
                    {
                        break;
                    }
                }
                //cdevice.Skt.Send(Encoding.UTF8.GetBytes("9,CR"));    //回复反馈一个确认，其实前面while里面有了足够的确认时间，此处可以省略网络卡的的回复
                #endregion

                Console.WriteLine("end ok  " + DateTime.Now);
            }
            else if (strRecive.Equals("HAVELABEL"))
            {
                Console.WriteLine("LABEL ok");
            }
            else if (strRecive.Equals("PICKLABEL"))   //取标一次，机械手就发一次信号
            {
                PrinterStatus = "PS";

                if (sktzebra != null)
                {
                    string ptcmd = "^XA~PS~HS^XZ";
                    ptcmd = "~PS~HS";
                    if (File.Exists("PS"))
                    {
                        sktzebra.Skt.Send(Encoding.UTF8.GetBytes(ptcmd));
                    }
                    else if (File.Exists("PSN"))
                    {
                        sktzebranet.Skt.Send(Encoding.UTF8.GetBytes(ptcmd));
                    }
                }
                else
                {
                    //btnManualPrintCtl(gwszebrea, strRecive);
                }

                #region 判断是不是overpack，如果是则切换5->6;
                string uwipchk = "$"; string Cartonchk = "$";
                if (autoscan_waitforprint(ref uwipchk, ref Cartonchk))
                {
                    if (Cartonchk == "5")
                    {
                        Console.WriteLine("找到DB中OVERPACK条码!-" + uwipchk + "Cartonchk=" + Cartonchk);
                        //UapdateMySqlteauto("autoscanlabel", "6");     //如果overpack是则切换5->6     
                    }
                }
                else
                {
                    Console.WriteLine("没找到DB中条码!-" + uwipchk + Cartonchk);
                }
                #endregion

                //mySerialPort.Write("^XA~PS^XZ");//可以出标
                Console.WriteLine("PS ok");
            }
            else if (strRecive.Equals("WHREQ"))               //吸标位 没有用
            {
                Console.WriteLine(strRecive);
                //cdevice.Skt.Send(Encoding.UTF8.GetBytes("0.1,0.2,0.3,0.4,0.5,2,1.5,CR"));    //传送坐标
                //cdevice.Skt.Send(Encoding.UTF8.GetBytes(File.ReadAllText(@"robotcmd.ini", Encoding.Default)));

                double dint = 0.0;
                int CFG = 2;
                double X = 483.000, Y = 330.000, Z = 69.000, C = 319.000, T = 0.000, SUM = 1203.000;
                string strtmp = X + "," + Y + "," + Z + "," + C + "," + T + "," + CFG + "," + SUM + ",CR";

                try
                {
                    #region XML操作模式                    
                    if (File.Exists("PTXYZCTCFG.xml"))       //配置XML
                    {
                        XmlDocument doc = new XmlDocument();
                        //加载要读取的XML
                        doc.Load("PTXYZCTCFG.xml");

                        //获得根节点
                        XmlElement RobotPoint = doc.DocumentElement;

                        //获得子节点 返回节点的集合
                        XmlNodeList xnl = RobotPoint.ChildNodes;

                        foreach (XmlNode item in xnl)
                        {
                            string strtemp = item.InnerText.Trim();
                            Console.WriteLine(item.Name + ":" + strtemp);
                            dint = Convert.ToDouble(strtemp);

                            if (item.Name.Equals("X"))
                            {
                                X = dint;
                            }
                            else if (item.Name.Equals("Y"))
                            {
                                Y = dint;
                            }
                            else if (item.Name.Equals("Z"))
                            {
                                Z = dint;
                            }
                            else if (item.Name.Equals("C"))
                            {
                                C = dint;
                            }
                            else if (item.Name.Equals("T"))
                            {
                                T = dint;
                            }
                            else if (item.Name.Equals("SUM"))
                            {
                                SUM = dint;
                            }
                            else if (item.Name.Equals("CFG"))
                            {
                                CFG = (int)dint;
                            }
                        }
                        if (SUM != X + Y + Z + C + T + CFG)
                        {
                            SUM = X + Y + Z + C + T + CFG;
                            Console.WriteLine("SUMNew=" + SUM);
                        }
                        strtmp = X + "," + Y + "," + Z + "," + C + "," + T + "," + CFG + "," + SUM + ",CR";
                    }
                    #endregion

                    #region 文件操作模式
                    else if (File.Exists("robotcmd.ini"))
                    {
                        strtmp = File.ReadAllText(@"robotcmd.ini", Encoding.Default);
                        string[] strtmps = strtmp.Split(',');
                        string strnew = "";
                        for (int i = 0; i < strtmps.Length - 2; i++)
                        {
                            strnew += strtmps[i] + ",";
                            dint += Convert.ToDouble(strtmps[i]);
                            Console.WriteLine("SUMNew=" + dint);
                        }
                        if (dint != Convert.ToDouble(strtmps[strtmps.Length - 2]))
                        {
                            strtmp = strnew + dint + ",CR";
                        }
                    }
                    #endregion
                }
                catch
                {
                    Console.WriteLine("发送坐标有异常!");
                }

                cdevice.Skt.Send(Encoding.UTF8.GetBytes(strtmp));
            }

            else if (strRecive.Equals("WHOK")) //没有
            {
                Console.WriteLine(strRecive);
            }
            else if (strRecive.Equals("WHNG"))//没有
            {
                Console.WriteLine(strRecive);
            }
            else if (strRecive.Equals("PTXYZCT"))         //贴label位  没有
            {
                Thread.Sleep(1000);          //20180612清理数据库后，发现机器手的翻转气缸没有垂直到位，机器手立即去执行贴标，所以暂时在此增加个延时等待气缸执行到位,暂定2s

                #region 数据库自适应模式
                string uwip = "", materialnumber = "", cmdinfo = "", brandnameTrayType = "";
                if (label_checkforstate_andnumber(ref uwip, ref materialnumber, ref cmdinfo, ref brandnameTrayType))
                {
                    if (brandnameTrayType.Equals("T450"))
                    {
                        if (File.Exists("PTXYZCT.ini"))
                        {
                            string ptx = File.ReadAllText("PTXYZCT.ini", Encoding.Default);
                            cdevice.Skt.Send(Encoding.UTF8.GetBytes(ptx + ",CR"));    //传送X坐标
                        }
                        else
                        {
                            cdevice.Skt.Send(Encoding.UTF8.GetBytes("17,CR"));    //传送X坐标
                        }
                    }
                    else
                    {
                        cdevice.Skt.Send(Encoding.UTF8.GetBytes("5,CR"));    //传送X坐标
                    }
                }
                else
                {
                    Console.WriteLine("数据库查询失败!8");
                    cdevice.Skt.Send(Encoding.UTF8.GetBytes("8,CR"));    //传送X坐标默认1/2                }
                }
                #endregion

                Console.WriteLine("PTX OK");
            }
            #endregion
        }

        static SerialPort mySerialPort;

        #region 自适应串口号
        /// <summary>
        /// 枚举win32 api
        /// </summary>
        public enum HardwareEnum
        {
            // 硬件
            Win32_Processor, // CPU 处理器
            Win32_PhysicalMemory, // 物理内存条
            Win32_Keyboard, // 键盘
            Win32_PointingDevice, // 点输入设备，包括鼠标。
            Win32_FloppyDrive, // 软盘驱动器
            Win32_DiskDrive, // 硬盘驱动器
            Win32_CDROMDrive, // 光盘驱动器
            Win32_BaseBoard, // 主板
            Win32_BIOS, // BIOS 芯片
            Win32_ParallelPort, // 并口
            Win32_SerialPort, // 串口
            Win32_SerialPortConfiguration, // 串口配置
            Win32_SoundDevice, // 多媒体设置，一般指声卡。
            Win32_SystemSlot, // 主板插槽 (ISA & PCI & AGP)
            Win32_USBController, // USB 控制器
            Win32_NetworkAdapter, // 网络适配器
            Win32_NetworkAdapterConfiguration, // 网络适配器设置
            Win32_Printer, // 打印机
            Win32_PrinterConfiguration, // 打印机设置
            Win32_PrintJob, // 打印机任务
            Win32_TCPIPPrinterPort, // 打印机端口
            Win32_POTSModem, // MODEM
            Win32_POTSModemToSerialPort, // MODEM 端口
            Win32_DesktopMonitor, // 显示器
            Win32_DisplayConfiguration, // 显卡
            Win32_DisplayControllerConfiguration, // 显卡设置
            Win32_VideoController, // 显卡细节。
            Win32_VideoSettings, // 显卡支持的显示模式。

            // 操作系统
            Win32_TimeZone, // 时区
            Win32_SystemDriver, // 驱动程序
            Win32_DiskPartition, // 磁盘分区
            Win32_LogicalDisk, // 逻辑磁盘
            Win32_LogicalDiskToPartition, // 逻辑磁盘所在分区及始末位置。
            Win32_LogicalMemoryConfiguration, // 逻辑内存配置
            Win32_PageFile, // 系统页文件信息
            Win32_PageFileSetting, // 页文件设置
            Win32_BootConfiguration, // 系统启动配置
            Win32_ComputerSystem, // 计算机信息简要
            Win32_OperatingSystem, // 操作系统信息
            Win32_StartupCommand, // 系统自动启动程序
            Win32_Service, // 系统安装的服务
            Win32_Group, // 系统管理组
            Win32_GroupUser, // 系统组帐号
            Win32_UserAccount, // 用户帐号
            Win32_Process, // 系统进程
            Win32_Thread, // 系统线程
            Win32_Share, // 共享
            Win32_NetworkClient, // 已安装的网络客户端
            Win32_NetworkProtocol, // 已安装的网络协议
            Win32_PnPEntity,//all device
        }
        /// <summary>
        /// Get the system devices information with windows api.
        /// </summary>
        /// <param name="hardType">Device type.</param>
        /// <param name="propKey">the property of the device.</param>
        /// <returns></returns>
        private static string[] GetHarewareInfo(HardwareEnum hardType, string propKey)
        {
            List<string> strs = new List<string>();
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from " + hardType))
                {
                    var hardInfos = searcher.Get();
                    foreach (var hardInfo in hardInfos)
                    {
                        if (hardInfo.Properties[propKey].Value != null)
                        {
                            //String str = hardInfo.Properties[propKey].Value.ToString(); strs.Add(str);
                            strs.Add(hardInfo.Properties[propKey].Value.ToString());    //同上简化
                        }
                    }
                }
                return strs.ToArray();
            }
            catch
            {
                return null;
            }
            finally
            {
                strs = null;
            }
        }//end of func GetHarewareInfo().

        static SerialPort mySerialPort2 = null;     //自定义串口实例
        static bool Myserialdevice()
        {
            #region 自动查找串口设备
            string com = "";
            try
            {
                string[] ss1 = GetHarewareInfo(HardwareEnum.Win32_PnPEntity, "Name");
                if (ss1.Length > 0)
                {
                    foreach (var item in ss1)  //ss1 or ss2 is same method
                    {
                        //if (item.Contains("Xenon 1900 Area-Imaging Scanner (COM") || item.Contains("Motorola Scanner Virtual USB COM Port (COM"))
                        if (item.Contains("Prolific USB-to-Serial Comm Port (COM"))
                        {
                            string[] comstrs = item.Split('(', ')');
                            com = comstrs[1];

                            mySerialPort2 = new SerialPort(com);
                            mySerialPort2.BaudRate = 9600;
                            mySerialPort2.Parity = Parity.None;
                            mySerialPort2.StopBits = StopBits.One;
                            mySerialPort2.DataBits = 8;
                            mySerialPort2.Handshake = Handshake.None;
                            mySerialPort2.RtsEnable = true;
                            mySerialPort2.Open();
                            if (mySerialPort2.IsOpen)
                            {
                                mySerialPort2.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler2);
                            }
                        }
                    }
                }
                if (com != "")
                {
                    //this.Text += "  " + com.ToUpper() + " MODE ON";
                    return true;
                }
                else
                {
                    //this.Text += "  " + com.ToUpper() + " MODE OFF";
                    return false;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
                //MessageBox.Show(err.ToString());
                mySerialPort2 = null;
                //this.Text += "  " + com.ToUpper() + " MODE OFF";
                return false;
            }
            #endregion
        }
        static void DataReceivedHandler2(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string Receivestr;
            //Receivestr = sp.ReadTo("\n");   //读到指定"\n"之前的所有字节,会分次数读入
            //Receivestr = sp.ReadExisting();  //读立即所有可用的字节，包括"\r","\n","\r\n"所有结尾的，需要测试而我LCD的双条码，经过测试"123\r\n456\r\n"一次读入，不符合拆分要求
        }
        #endregion

        static int processID;
        static DateTime dtPause;
        static TimeSpan tsPause;
        static List<string> lstsiteOverpack = null;
        static List<string> lstsiteTW = null;
        static List<string> lstmectl = null;
        //测试变量
        static Cdevice skttiebiao = null;
        static Cdevice skttiebiaoscan = null;
        static Cdevice sktzebra = null;
        static Cdevice sktzebranet = null;
        static Cdevice sktstorgescan = null;  //立库扫描
        static string bakmtsn = string.Empty;      //备份扫描流水号比较不能两台连续相同

        //数据库为主，盘片为辅，输出三种状态，和相关卡夹和贴单等信息
        public static Result_back.LabelResult number_labeltopaste(string strRecive, out string mtsnfront, out string brandname, out Result_back.LabelNumber checkFlg)
        {
            Result_back.LabelResult resprocess = Result_back.LabelResult.normal;
            checkFlg = Result_back.LabelNumber.pre_labeling;   //贴单数量标记
            mtsnfront = "";
            brandname = "";
            try
            {
                string info = "";
                if (strRecive.Substring(0, 2).Equals("1S"))    //读到条码
                {
                    mtsnfront = strRecive.Substring(0, 20);  //mtsn = strRecive; //里面包含回车符
                    brandname = mtsnfront.Substring(2, 4);
                    string model = mtsnfront.Substring(6, 3);
                    string sn = mtsnfront.Substring(12);

                    Console.WriteLine("扫描的条码信息:" + mtsnfront);

                    bool MEmanaul = false, overpack = false, bulckpack = false, taiwan = false, cto = false, mrp = false, asset = false;   //类型标记 默认初始化是false

                    if (brandname.Equals("20FM") || brandname.Equals("20FN"))
                    {
                        brandname = "T460";
                    }
                    else if (brandname.Equals("20F9") || brandname.Equals("20FA"))
                    {
                        brandname = "T460s";
                    }
                    else if (brandname.Equals("20HD") || brandname.Equals("20HE") || brandname.Equals("20JM") || brandname.Equals("20JN"))   //第三种卡夹PN
                    {
                        brandname = "T470";
                    }
                    else if (brandname.Equals("20HF") || brandname.Equals("20HG") || brandname.Equals("20JS") || brandname.Equals("20JT"))
                    {
                        brandname = "T470s";
                    }
                    else if (brandname.Equals("20L7") || brandname.Equals("20L8"))      //卡夹不同
                    {
                        brandname = "T480s";
                    }
                    else if (brandname.Equals("20NX") || brandname.Equals("20NY"))      //卡夹不同
                    {
                        brandname = "T490s";
                    }
                    else
                    {
                        //brandname = string.Empty;
                        checkFlg = Result_back.LabelNumber.no_labeling;  //MTM not pick
                    }                    

                    #region 第二种模式-多标记-模块组合化
                    if (checkFlg == Result_back.LabelNumber.pre_labeling)
                    {
                        string[] bomstrlines = null;
                        string mtsnpath = @"\\cml2s460\dfcxact\mtsn\" + sn;

                        #region SO和MO信息及COUNTRY信息
                        string so = "", mo = "", boms = "";
                        string country = "";        //PRODUCT_COUNTRY=IN
                        //bool moflg = true;
                        bool mosocountryflg = true;
                        bool mobomflg = true;
                        if (!SelectMySqlMerwebMOBOM(mtsnfront, out mo, out boms))
                        {
                            Console.WriteLine("MO 信息不全!,BOM信息不全!");
                            info += "MO 信息不全!,BOM信息不全!";
                            mobomflg = false;
                        }
                        else
                        {
                            if (!SelectMySqlTeautoSOMOCOUNTRY(mo, out so, out country))
                            {
                                Console.WriteLine("MO,SO,COUNTRY 信息不全!");
                                info += "MO,SO,COUNTRY 信息不全!";
                                mosocountryflg = false;
                            }
                            else
                            {
                                bomstrlines = boms.Split('\n');
                            }
                        }

                        if (!mobomflg)
                        {
                            try
                            {
                                bomstrlines = File.ReadAllLines(mtsnpath + "\\bom.out");
                            }
                            catch
                            {
                                File.AppendAllText("DataLog.txt", info + ", bom盘片数据空，记录时间:" + DateTime.Now.ToString(), Encoding.Default);
                            }
                        }

                        if (!mosocountryflg)
                        {
                            if (SelectMySqlMerwebSoContry(mo, out so, out country))
                            {
                                Console.WriteLine("MO, SO, COUNTRY 信息:" + mo + ":" + so + ":" + country);
                            }
                            else
                            {
                                try
                                {
                                    string[] somo = File.ReadAllLines(mtsnpath + "\\MO_SN.BAT");  //盘片MO_SN.BAT中获取方式SO,MO
                                    foreach (string line in somo)
                                    {
                                        if (line.Contains("REM SO~"))
                                        {
                                            so = line.Split('~')[1].Trim();
                                        }
                                        if (line.Contains("REM MO~"))
                                        {
                                            mo = line.Split('~')[1].Trim();
                                        }
                                    }

                                    string[] strpicis = File.ReadAllLines(mtsnpath + "\\PICI.INI");  //盘片PICI.INI中获取方式PRODUCT_COUNTRY
                                    foreach (string line in strpicis)
                                    {
                                        if (line.Equals("PRODUCT_COUNTRY="))
                                        {
                                            country = line.Split('=')[1]; break;
                                        }
                                        else if (line.Equals("[BOM]"))
                                        {
                                            break;
                                        }
                                    }
                                }
                                catch
                                {
                                    File.AppendAllText("DataLog.txt", info + ", 有空数据，记录时间:" + "SO: " + so + ", MO: " + mo + ", COUNTRY: " + country + " " + DateTime.Now.ToString(), Encoding.Default);
                                }
                            }
                        }

                        info += "\r\nSO:" + so + ",MO:" + mo + ",COUNTRY:" + country;
                        if (mo == "" || so == "" || country == "")  //记录SO或者country日志
                        {
                            Console.WriteLine(info);
                            File.AppendAllText("DataLog.txt", info + ", 有空数据，记录时间:" + DateTime.Now.ToString(), Encoding.Default);
                        }

                        if (mo != "")
                        {
                            if (!dicmtsnmo.ContainsKey(mtsnfront))      //没有重复的key 
                            {
                                //Console.WriteLine("Key \"key1\" is not found.");
                                dicmtsnmo.Add(mtsnfront, mo);
                            }
                        }
                        #endregion
                        
                        #region ME手工自定义类型判断-是否Overpack和打单数量
                        /*
                        if (mo != "" && lstmectl.Count > 0)
                        {
                            for (int i = 0; i < lstmectl.Count; i++)
                            {
                                if (lstmectl[i].Contains(mo))
                                {
                                    MEmanaul = true;
                                    if (lstmectl[i].Split(':')[1].ToUpper().Equals("Y"))
                                    {
                                        overpack = true;
                                    }
                                    checkFlg =(Result_back.LabelNumber) Convert.ToInt32(lstmectl[i].Split(':')[2]);
                                    info += "ME手工定义类型:" + lstmectl[i];
                                    break;
                                }
                            }
                        }
                        */
                        #endregion
                    
                        #region overpack判断
                        if (lstsiteOverpack.Count > 0)
                        {
                            for (int i = 0; i < lstsiteOverpack.Count; i++)
                            {
                                foreach (string line in bomstrlines)
                                {
                                    if (lstsiteOverpack[i].Equals(line.Split('~')[1]))
                                    {
                                        overpack = true;
                                        info += "机器流水号:" + mtsnfront + ",PN号:" + line.Split('~')[1] + ", Overpack双包机型!";
                                        break;
                                    }
                                }
                                if (overpack) break;
                            }
                        }
                        #endregion

                        #region Taiwan类型的过滤判断
                        if (lstsiteTW.Count > 0)
                        {
                            for (int i = 0; i < lstsiteTW.Count; i++)
                            {
                                foreach (string line in bomstrlines)
                                {
                                    if (lstsiteTW[i].Equals(line.Split('~')[1]))
                                    {
                                        taiwan = true;
                                        info += "机器流水号:" + mtsnfront + ",PN号:" + line.Split('~')[1] + ", TaiWan机型!";
                                        break;
                                    }
                                }
                            }
                        }
                        #endregion

                        #region Bulk Pack类型的过滤判断
                        foreach (string line in bomstrlines)
                        {
                            if ("BK_PACK".Equals(line.Split('~')[0]) && "1".Equals(line.Split('~')[2]) && "PKGM".Equals(line.Split('~')[3]))    //BK_PACK~75Y4888~1~PKGM
                            {
                                bulckpack = true;
                                info += "机器流水号:" + mtsnfront + ",PN号:" + line.Split('~')[1] + ", Bulk Pack类型!";
                                break;
                            }
                        }
                        #endregion

                        #region 带资产机型的过滤判断
                        //这个逻辑是判断所有的资产标签，包括总装的，如果只需要外箱贴资产标签的PN，参考文件\\CML2S460\tables\ASSET_PKG.DAT
                        foreach (string line in bomstrlines)
                        {
                            if ("ASSETTAG".Equals(line.Split('~')[3]))     //待测试  KITTING~0C18439~1~ASSETTAG~N~N~Y~N~N~N~Y   -》ok
                            {
                                asset = true;
                                if (asset)
                                    info += "机器流水号:" + mtsnfront + ",PN号:" + line.Split('~')[3] + ", 带资产打印校验类型!";
                                break;
                            }
                        }
                        #endregion

                        #region CTO过滤判断
                        if (model.Equals("CTO"))        //CTO判断
                        {
                            cto = true;
                        }
                        #endregion

                        #region India MRP 判断                        
                        if (country.Equals("IN"))                   //India MRP 判断, 第三种方式，如果是MRP则一定是India和422开头，无法加bom控制, 防止异常
                        {
                            if (so != "" && so.Length > 3)
                            {
                                if (so.Substring(0, 3).Equals("422"))
                                {
                                    mrp = true;
                                }
                            }
                        }
                        #endregion

                        #region 综合判断结果输出
                        if (mo == "")
                        {
                            resprocess = Result_back.LabelResult.fail;
                        }
                        else if (MEmanaul)
                        {
                            if (overpack)
                            {
                                string strmode = File.ReadAllText("mode.txt", Encoding.Default);
                                if (strmode.Equals("1"))    //非双包
                                {
                                    resprocess = Result_back.LabelResult.fail;
                                }
                                else                //否则"2" 双包类型
                                {
                                    checkFlg = Result_back.LabelNumber.overpack;       //overpack新标记
                                    resprocess = Result_back.LabelResult.normal;
                                }
                            }
                            else
                            {
                                resprocess = Result_back.LabelResult.normal;  //checkFlg已经从前面文件读出
                            }
                        }
                        else if (bulckpack)
                        {
                            checkFlg = Result_back.LabelNumber.pre_labeling;       //bulckpack新标记

                            if (File.Exists("bulckpack"))
                            {
                                resprocess = Result_back.LabelResult.normal;     //新模式支持
                            }
                            else
                            {
                                resprocess = Result_back.LabelResult.fail;         //旧模式不支持bulckpack
                            }
                        }
                        else if (taiwan && File.Exists("TW.TXT"))     //拦截taiwan
                        {
                            resprocess = Result_back.LabelResult.fail;
                        }
                        else
                        {
                            if (overpack)
                            {
                                string strmode = File.ReadAllText("mode.txt", Encoding.Default);
                                if (strmode.Equals("1"))    //非双包
                                {
                                    resprocess = Result_back.LabelResult.fail;
                                }
                                else                //否则"2" 双包类型
                                {
                                    if (taiwan)
                                    {
                                        if (brandname.Equals("T470s") || brandname.Equals("T470") || brandname.Equals("T480s"))          //新产品在CTL工位不用在袋子上粘贴和扫描TW label
                                        {
                                            checkFlg = 5;       //overpack新标记
                                            resprocess = Result_back.LabelResult.normal;
                                        }
                                        else
                                        {
                                            resprocess = Result_back.LabelResult.normal;
                                            info += "机器流水号:" + mtsnfront + ",旧产品Taiwan机型双包，不能做!";
                                        }
                                    }
                                    else
                                    {
                                        checkFlg = 5;       //overpack新标记
                                        resprocess = Result_back.LabelResult.normal;
                                    }
                                }
                            }
                            else
                            {
                                if (taiwan)
                                {
                                    if (brandname.Equals("T470s") || brandname.Equals("T470") || brandname.Equals("T480s"))          //新产品在CTL工位不用在袋子上粘贴和扫描TW label
                                    {
                                        taiwan = true;
                                    }
                                    else
                                    {
                                        taiwan = false;
                                    }
                                }

                                //分析处理贴单数量
                                if (taiwan && cto)
                                {
                                    checkFlg = Result_back.LabelNumber.three;
                                    info += "机器流水号:" + mtsnfront + ",TaiWan and CTO Both!";
                                }
                                else if (taiwan)
                                {
                                    checkFlg = Result_back.LabelNumber.two;
                                    info += "机器流水号:" + mtsnfront + ",TaiWan only!";
                                }
                                else if (cto)
                                {
                                    checkFlg = Result_back.LabelNumber.two;
                                    info += "机器流水号:" + mtsnfront + ",CTO only!";
                                }
                                else if (mrp)
                                {
                                    checkFlg = Result_back.LabelNumber.two;
                                    info += "机器流水号:" + mtsnfront + ",India MRP only!";
                                }
                                else
                                {
                                    checkFlg = Result_back.LabelNumber.one;
                                    info += "机器流水号:" + mtsnfront + ",others normal only!";
                                }

                                resprocess = Result_back.LabelResult.normal;
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        resprocess = Result_back.LabelResult.fail;
                        info += "\r\n机器流水号:" + mtsnfront + ",非T460,T460s,T470s带卡夹机型!";                        
                    }
                    #endregion
                }
                else if (strRecive.Substring(0, 6).Equals("NOREAD"))  //没读到条码
                {
                    resprocess = Result_back.LabelResult.noread_others;
                    info += "\r\n机器流水号:" + "NOREAD";
                }
                else
                {
                    resprocess = Result_back.LabelResult.noread_others;
                    info += "\r\n扫描数据:" + strRecive;
                }

                //综合集中处理显示和记录日志信息
                Console.WriteLine(info);
                File.AppendAllText("DataLog.txt", info + ", 记录时间:" + DateTime.Now.ToString(), Encoding.Default);
            }
            catch (Exception err)
            {
                resprocess = Result_back.LabelResult.fail;
                Console.WriteLine(err);
                File.AppendAllText("error.log", err.ToString() + "记录时间:" + DateTime.Now.ToString(), Encoding.Default);
            }
            return resprocess;
        }

        static Dictionary<string, string> dicmtsnmo = new Dictionary<string, string>();

        private static void btnManualPrintCtl(string zebraip, string cmd)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo ps = new System.Diagnostics.ProcessStartInfo("cmd");
                ps.UseShellExecute = false;
                ps.CreateNoWindow = true;
                ps.RedirectStandardOutput = true;
                ps.RedirectStandardInput = true;
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(ps);

                string strpt;
                if (cmd.Equals("PP") || cmd.Equals("PS") || cmd.Equals("JA") || cmd.Equals("HS"))
                {
                    //strpt = @"ECHO ^XA~" + cmd + @"^XZ > \\" + zebraip + @"\ZEBRASERNET";
                    strpt = @"ECHO ^XA~" + cmd + @"^XZ > \\" + zebraip + @"\ZEBRASER";
                    p.StandardInput.WriteLine(strpt);
                    //Console.WriteLine(strpt);
                    PrinterStatus = cmd;
                    File.AppendAllText("printer.csv", "\r\n" + PrinterStatus + "," + DateTime.Now.ToString(), Encoding.Default);
                }
                //else
                //{
                //    strpt = @"ECHO ^XA~PS^XZ > \\" + zebraip + @"\ZEBRASERNET";
                //    p.StandardInput.WriteLine(strpt);   //启动打印
                //    Console.WriteLine(strpt);
                //    strpt = @"COPY " + cmd + @" \\" + zebraip + @"\ZEBRASERNET";
                //    p.StandardInput.WriteLine(strpt);   //打印label
                //    Console.WriteLine(strpt);
                //}
                Thread.Sleep(200);
                p.StandardInput.WriteLine(@"exit");
                p.WaitForExit();//之前要记住exit，否则会持续等待
            }
            catch
            {
                Console.WriteLine("执行打印机控制失败！");
                File.AppendAllText("PinterControl.log", "\r\n执行打印机控制失败！" + DateTime.Now.ToString(), Encoding.Default);
            }
        }

        private static void PingDevice(object obj)  //检查各个客户端ping心跳
        {
            ThreadCdevice threadcdevice = (ThreadCdevice)obj;
            while (true)
            {
                Thread.Sleep(5000);
                Ping pingSender = new Ping();
                int cnt = 0;
                PingReply reply = pingSender.Send(threadcdevice.cdevice.Ip, 220);
                if (reply.Status == IPStatus.Success) cnt++;

                Thread.Sleep(1000);
                reply = pingSender.Send(threadcdevice.cdevice.Ip, 220);
                if (reply.Status == IPStatus.Success) cnt++;

                Thread.Sleep(1000);
                reply = pingSender.Send(threadcdevice.cdevice.Ip, 220);
                if (reply.Status == IPStatus.Success) cnt++;

                if (cnt > 1)
                {
                    //File.AppendAllText(threadcdevice.cdevice.Dvid + "run.log", "\r\n" + threadcdevice.cdevice.Dvid + "  " + threadcdevice.cdevice.Ip + "网络连接通讯心跳检测正常在线!" + DateTime.Now.ToString(), Encoding.Default);
                }
                else    //ping不通 
                {
                    Console.WriteLine(threadcdevice.cdevice.Dvid + "  " + threadcdevice.cdevice.Ip + "异常掉线!启动网络重连..." + DateTime.Now.ToString());
                    File.AppendAllText(threadcdevice.cdevice.Dvid + "error.log", "\r\n" + threadcdevice.cdevice.Dvid + "  " + threadcdevice.cdevice.Ip + "网络连接通讯心跳检测掉线!自动重新连接尝试进行中..." + DateTime.Now.ToString(), Encoding.Default);
                    threadcdevice.thread.Abort();
                    threadcdevice.thread = null; threadcdevice.cdevice.Skt = null;//清除线程和socket
                    Thread.Sleep(5000);
                    Console.WriteLine(threadcdevice.cdevice.Dvid + "  " + threadcdevice.cdevice.Ip + "网络线程重新开启!" + DateTime.Now.ToString());
                    threadcdevice.thread = new Thread(ListenDevice);           //网络通信接收
                    threadcdevice.thread.IsBackground = true;
                    threadcdevice.thread.Start(threadcdevice.cdevice);
                }
            }
        }
        public static string Chr(int asciiCode)   //C# ASCII转字符
        {
            if (asciiCode >= 0 && asciiCode <= 255)
            {
                System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
                byte[] byteArray = new byte[] { (byte)asciiCode };
                string strCharacter = asciiEncoding.GetString(byteArray);
                return (strCharacter);
            }
            else
            {
                throw new Exception("ASCII Code is not valid.");
            }
        }

        public static string MySqlStringonline = @"Server=10.186.242.64;Database=lenwh;Uid=lenUser;Pwd=lenUser!1234";   //立库

        //new
        public static string MySqlString = @"Server=10.186.204.64;Database=prom;Uid=wanshan;Pwd=2ASRIYhIl$E";
        public static string MySqlStringteauto = @"Server=10.186.204.64;Database=teauto;Uid=wanshan;Pwd=2ASRIYhIl$E";    //infosmart


        private static bool SelectMySqlMerwebMOBOM(string mtsnfront, out string mo, out string bom)
        {
            mo = ""; bom = "";
            //string SqlCmdString = "SELECT ORDER_Num,OP_Type_Name,Material_Name,Quantity,Material_Category_Name,MATERIAL_ACTION,IBMSNAP_OPERATION FROM order_bom WHERE IBMSNAP_OPERATION != 'D' AND ORDER_Num = (SELECT ORDER_NUM FROM order_sn WHERE UWIP_BARCODE = '" + mtsnfront + "')";
            //MATERIAL_ACTION !='ADD' and MATERIAL_ACTION !='SECURE' AND MATERIAL_ACTION !='REMOVE' 有三种动作状态
            string SqlCmdString = "SELECT ORDER_Num,OP_Type_Name,Material_Name,Quantity,Material_Category_Name,MATERIAL_ACTION,OP_TYPE_NAME FROM order_bom WHERE MATERIAL_ACTION != 'REMOVE' AND ORDER_Num = (SELECT ORDER_NUM FROM order_sn WHERE UWIP_BARCODE = '" + mtsnfront + "')";

            MySqlConnection conn = new MySqlConnection(MySqlString);
            MySqlCommand cmd;
            MySqlDataReader rd = null;

            try   //DB Query
            {
                conn.Open();
                cmd = new MySqlCommand(SqlCmdString, conn);
                rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    mo = rd[0].ToString();
                    //bom += rd[1].ToString() + rd[2].ToString() + rd[3].ToString() + rd[4].ToString() + rd[5].ToString() + rd[6].ToString() + "\n";
                    for (int i = 1; i < 7; i++)
                    {
                        bom += rd[i].ToString() + "~";
                    }
                    bom += "\n";
                }

                if (bom != "")
                {
                    bom = bom.Remove(bom.Length - 1);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception err)
            {
                File.AppendAllText("system.log", "\r\n" + err.ToString());      //写入日志
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        private static bool SelectMySqlMerwebSoContry(string mo, out string so, out string country)
        {
            so = ""; country = "";
            string SqlCmdString = "SELECT * FROM order_info WHERE  ORDER_NUM = '" + mo + "')";
            MySqlConnection conn = new MySqlConnection(MySqlString);
            MySqlCommand cmd;
            MySqlDataReader rd = null;

            try   //DB Query
            {
                conn.Open();
                cmd = new MySqlCommand(SqlCmdString, conn);
                rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    if (rd["ORDER_ATTRIBUTE"].ToString().Equals("SO_NUM"))
                    {
                        so = rd["ORDER_VALUE"].ToString();
                    }
                    else if (rd["ORDER_ATTRIBUTE"].ToString().Equals("Z_COUNTRY"))
                    {
                        country = rd["ORDER_VALUE"].ToString();
                    }
                }

                if (so != "" || country != "")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception err)
            {
                File.AppendAllText("system.log", "\r\n" + err.ToString());      //写入日志
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        private static bool SelectMySqlTeautoSOMOCOUNTRY(string mo, out string so, out string country)
        {
            so = ""; country = "";
            string SqlCmdString = string.Format("SELECT * FROM {0} WHERE mo = '{1}' limit 1", "teauto.mo_so_country", mo);
            MySqlConnection conn = new MySqlConnection(MySqlStringteauto);
            try   //DB Query
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SqlCmdString, conn);
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    so = rd["so"].ToString();
                    country = rd["country"].ToString();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        static bool InsertMySqlteauto(string tabname, string mtsn, int weight)         //通用
        {
            MySqlConnection conn = new MySqlConnection(MySqlStringteauto);
            string SqlCmdString = string.Format("INSERT INTO {0}(MTSN,Weight,DTime) VALUES('{1}','{2}','{3}')", tabname, mtsn, weight, DateTime.Now.ToString("yyyyMMddHHmmss"));
            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SqlCmdString, conn);

                if (cmd.ExecuteNonQuery() > 0)
                {
                    Console.WriteLine("插入成功");
                    return true;  //插入成功
                }
                else
                {
                    Console.WriteLine("插入失败");
                    return false;  //插入失败
                }
            }
            catch (Exception err)
            {
                File.AppendAllText("sysmsg.log", "\r\n " + DateTime.Now.ToString() + "  Error!\r\n" + err.ToString(), Encoding.Default);  //写入日志
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        static bool queue_firstinsert(string tabname, string mtsn, string brandname, int cartonqty)    //卡夹打单信息入库队列
        {
            MySqlConnection conn = new MySqlConnection(MySqlStringteauto);

            //string SqlCmdString = string.Format("INSERT INTO {0}(MTSN,DTimeFirst,TrayType,CartonLabel) VALUES('{1}','{2}','{3}','{4}')", tabname, mtsn, DateTime.Now.ToString("yyyyMMddHHmmss"), brandname, cartonqty);
            string SqlCmdString = string.Format("INSERT INTO {0}(MTSN,DTimeFirst,TrayType,CartonLabel,Line,Date_Time) VALUES('{1}','{2}','{3}','{4}','KIT202','{5}')", tabname, mtsn, DateTime.Now.ToString("yyyyMMddHHmmss"), brandname, cartonqty, DateTime.Now.ToString());

            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SqlCmdString, conn);

                if (cmd.ExecuteNonQuery() > 0)
                {
                    //Console.WriteLine("插入成功");
                    return true;  //插入成功
                }
                else
                {
                    Console.WriteLine("插入失败");
                    File.AppendAllText("queue_firstinsert.log", "\r\n 插入失败!" + DateTime.Now.ToString(), Encoding.Default);  //写入日志
                    return false;  //插入失败
                }
            }
            catch (Exception err)
            {
                File.AppendAllText("queue_firstinsert.log", "\r\n " + DateTime.Now.ToString() + "  Error!\r\n" + err.ToString(), Encoding.Default);  //写入日志
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        static bool verticalbank_insert(string mtsn)    //卡夹打单信息入库队列,调用存储过程
        {
            MySqlConnection conn = new MySqlConnection(MySqlStringonline);
            string SqlCmdString = string.Format("SELECT NewWHOnlineData('" + mtsn + "')");
            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SqlCmdString, conn);
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    if (rd[0].ToString() == "1")
                    {
                        //Console.WriteLine(1);
                        return true;
                    }
                }
                File.AppendAllText("InsertMySqlonline.log", "\r\n 数据库异常，" + mtsn + "," + DateTime.Now.ToString(), Encoding.Default);  //写入日志
                return false;
            }
            catch (Exception)
            {
                File.AppendAllText("InsertMySqlonline.log", "\r\n 数据库异常，" + mtsn + "," + DateTime.Now.ToString(), Encoding.Default);  //写入日志
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        static bool SelectMySqlteautoPPOK()    //查PPOK
        {
            MySqlConnection conn = new MySqlConnection(MySqlStringteauto);
            string SqlCmdString = string.Format("SELECT * FROM {0} WHERE Appsetting = 'PPOK' LIMIT 1", "appconfig");
            try   //DB Query
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SqlCmdString, conn);
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    if (rd["Value1"].ToString().Equals("Y"))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    File.AppendAllText("SelectMySqlteautoPPOK.log", "\r\n 查询标记失败!" + DateTime.Now.ToString(), Encoding.Default);
                    Console.WriteLine("没查到数据!");
                    return false;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
                File.AppendAllText("SelectMySqlteautoPPOK.log", "\r\n 查询标记失败!" + DateTime.Now.ToString(), Encoding.Default);
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        static bool UapdateMySqlteautoPPOK(string cmdinfo)         //更新PPOK
        {
            MySqlConnection conn = new MySqlConnection(MySqlStringteauto);
            string SqlCmdString = string.Format("UPDATE {0} SET Value1 = '{1}' WHERE Appsetting = 'PPOK'", "appconfig", cmdinfo);
            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SqlCmdString, conn);

                if (cmd.ExecuteNonQuery() > 0)
                {
                    Console.WriteLine("PPOK更新成功");
                    return true;
                }
                else
                {
                    Console.WriteLine("PPOK更新失败");
                    return false;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        static bool label_checkforstate_andnumber(ref string uwip, ref string materialnumber, ref string cmdinfo, ref string brandnameTrayType)    //查CartonLabel信息和贴单标记仅仅一台缓存队列
        {
            MySqlConnection conn = new MySqlConnection(MySqlStringteauto);
            string SqlCmdString = string.Format("SELECT MTSN,CartonLabel,DTimeThird,TrayType FROM {0} WHERE CartonLabel IS NOT NULL AND DTimeThird IS NOT NULL AND  DTimeFourth IS NULL LIMIT 1", "autoscanlabel");
            try   //DB Query
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SqlCmdString, conn);
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    uwip = rd[0].ToString();
                    materialnumber = rd[1].ToString();
                    cmdinfo = rd[2].ToString();
                    brandnameTrayType = rd["TrayType"].ToString();
                    return true;
                }
                else
                {
                    File.AppendAllText("label_checkforstate_andnumber.log", "\r\n 查询DTimeThird 标记失败!,指令是:" + cmdinfo + ", " + DateTime.Now.ToString(), Encoding.Default);
                    Console.WriteLine("没查到数据!");
                    return false;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
                File.AppendAllText("label_checkforstate_andnumber.log", "\r\n 查询DTimeThird 标记失败!,指令是:" + cmdinfo + ", " + DateTime.Now.ToString(), Encoding.Default);
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        static bool autoscan_waitforprint(ref string uwip, ref string CartonLabelqty)    //查uwip信息校验扫描
        {
            MySqlConnection conn = new MySqlConnection(MySqlStringteauto);
            string SqlCmdString = string.Format("SELECT MTSN,CartonLabel FROM {0} WHERE CartonLabel IS NOT NULL AND DTimeFourth IS NULL LIMIT 1", "autoscanlabel");
            try   //DB Query
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SqlCmdString, conn);
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    uwip = rd[0].ToString();
                    CartonLabelqty = rd[1].ToString();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        static bool UapdateMySqlteauto(string tabname, string cmdinfo)         //更新PROM打单或发送贴单机器人完成时间信息
        {
            MySqlConnection conn = new MySqlConnection(MySqlStringteauto);
            string SqlCmdString = "";

            if (cmdinfo.Equals("6"))        //针对overpack切换
            {
                SqlCmdString = string.Format("UPDATE {0} SET CartonLabel = '{1}' WHERE CartonLabel IS NOT NULL AND DTimeFourth IS NULL LIMIT 1", tabname, cmdinfo);
            }
            else if (cmdinfo.Equals("RC"))
            {
                SqlCmdString = string.Format("UPDATE {0} SET DTimeThird = '{1}' WHERE CartonLabel IS NOT NULL AND DTimeThird IS NULL LIMIT 1", tabname, DateTime.Now.ToString("yyyyMMddHHmmss"));
            }
            else if (cmdinfo.Equals("ED"))
            {
                SqlCmdString = string.Format("UPDATE {0} SET DTimeFourth = '{1}' WHERE CartonLabel IS NOT NULL AND DTimeFourth IS NULL LIMIT 1", tabname, DateTime.Now.ToString("yyyyMMddHHmmss"));
            }
            else if (cmdinfo.Equals("PRINT"))   //发送打单
            {
                //SqlCmdString = string.Format("UPDATE {0} SET DTimeThird = '{1}' WHERE CartonLabel IS NOT NULL AND DTimeThird IS NULL LIMIT 1", tabname, cmdinfo);
                SqlCmdString = string.Format("UPDATE {0} SET DTimeSecond = '{1}',DTimeThird = '{2}' WHERE CartonLabel IS NOT NULL AND DTimeThird IS NULL LIMIT 1", tabname, DateTime.Now.ToString("yyyyMMddHHmmss"), cmdinfo);
            }
            else if (cmdinfo.Equals("ROBOTCANCEL"))   //取消贴标
            {
                SqlCmdString = string.Format("UPDATE {0} SET DTimeThird = '{1}' WHERE CartonLabel IS NOT NULL AND DTimeThird ='PRINT' LIMIT 1", tabname, cmdinfo);
            }
            else if (cmdinfo.Equals("ROBOT"))   //发送贴标
            {
                SqlCmdString = string.Format("UPDATE {0} SET DTimeThird = '{1}' WHERE CartonLabel IS NOT NULL AND DTimeThird ='PRINT' LIMIT 1", tabname, DateTime.Now.ToString("yyyyMMddHHmmss"));
            }
            else if (cmdinfo.Equals("CLR1"))
            {
                SqlCmdString = string.Format("UPDATE {0} SET DTimeFirst = '{1}' WHERE DTimeFirst IS NULL", tabname, "T");
            }
            else if (cmdinfo.Equals("CLR2"))
            {
                SqlCmdString = string.Format("UPDATE {0} SET DTimeSecond = '{1}' WHERE DTimeSecond IS NULL", tabname, "T");
            }
            else if (cmdinfo.Equals("CLR3"))
            {
                SqlCmdString = string.Format("UPDATE {0} SET DTimeThird = '{1}' WHERE DTimeThird IS NULL", tabname, "T");
            }
            else if (cmdinfo.Equals("CLR4"))
            {
                SqlCmdString = string.Format("UPDATE {0} SET DTimeFourth = '{1}' WHERE DTimeFourth IS NULL", tabname, "T");
            }
            else if (cmdinfo.Equals("CLR5"))   //BatterySN
            {
                SqlCmdString = string.Format("UPDATE {0} SET BatterySN = '{1}' WHERE BatterySN IS NULL", tabname, "T");
            }
            else if (cmdinfo.Equals("CLR6"))   //Storage
            {
                SqlCmdString = string.Format("UPDATE {0} SET Storage = '{1}' WHERE Storage IS NULL", tabname, "T");
            }
            else if (cmdinfo.Equals("STG"))
            {
                SqlCmdString = string.Format("UPDATE {0} SET Storage = '{1}' WHERE Storage IS NULL", tabname, "STG");
            }
            else if (cmdinfo.Equals("CANCEL"))
            {
                SqlCmdString = string.Format("UPDATE {0} SET Storage = '{1}' WHERE Storage IS NULL", tabname, "CANCEL");
            }
            else if (cmdinfo.Equals("qty_chg_add"))
            {
                SqlCmdString = string.Format("UPDATE {0} SET qty_chg_add = (qty_chg_add +1) WHERE MO ='", tabname, "CANCEL");
            }
            else
            {
                return false;
            }

            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SqlCmdString, conn);

                if (cmd.ExecuteNonQuery() > 0)
                {
                    //Console.WriteLine("更新成功");
                    return true;
                }
                else
                {
                    Console.WriteLine("更新:" + cmdinfo);
                    File.AppendAllText("UapdateMySqlteauto.log", "\r\n更新DTimeThird 标记失败!,指令是:" + cmdinfo + ", " + DateTime.Now.ToString(), Encoding.Default);
                    return false;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        static bool UapdateMySqlteauto(string tabname, string cmdinfo, string MO)         //更新预加工反向计数信息
        {
            MySqlConnection conn = new MySqlConnection(MySqlStringteauto);
            string SqlCmdString = "";

            if (cmdinfo.Equals("qty_chg_add"))
            {
                SqlCmdString = string.Format("UPDATE {0} SET qty_chg_add = (qty_chg_add +1) WHERE MO ='{1}'", tabname, MO);
            }
            else
            {
                return false;
            }

            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SqlCmdString, conn);

                if (cmd.ExecuteNonQuery() > 0)
                {
                    //Console.WriteLine("更新成功");
                    return true;
                }
                else
                {
                    Console.WriteLine("更新:" + cmdinfo);
                    File.AppendAllText("UapdateMySqlteauto.log", "\r\n更新标记失败!,指令是:" + SqlCmdString + ", " + DateTime.Now.ToString(), Encoding.Default);
                    return false;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        static int selectqueue_once()    //查uwip信息校验扫描
        {
            MySqlConnection conn = new MySqlConnection(MySqlStringteauto);
            string SqlCmdString = string.Format("SELECT * FROM {0} WHERE DTimeFourth IS NULL", "autoscanlabel");
            try   //DB Query
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SqlCmdString, conn);
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    return 0;  //成功
                }
                else
                {
                    return 1;  //无数据
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
                return -1;  //数据库错误
            }
            finally
            {
                conn.Close();
            }
        }

        static int Stereolib_queue()    //查uwip信息校验扫描
        {
            MySqlConnection conn = new MySqlConnection(MySqlStringonline);
            string SqlCmdString = string.Format("SELECT * FROM {0} WHERE iAccessoryPickStatus <>'6' and dOnlineTime >= CURDATE() LIMIT 1", "packend");
            try   //DB Query
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SqlCmdString, conn);
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    return 0;  //成功
                }
                else
                {
                    return 1;  //无数据
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
                return -1;  //数据库错误
            }
            finally
            {
                conn.Close();
            }
        }
    }
}