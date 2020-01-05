using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Apiabpick
{
    class Abpick
    {
        public struct Diagnosis
        {
            public string[] Port;

            public void Init(int count)
            {
                Port = new string[count];
            }
        }

        Thread threadRcv;
        bool threadRcvflg = true;

        delegate void ShowMsgCallback(string text);

        const int SUMCMD_CONFIRM_BUTTON = 6;
        const int SUBCMD_SHORTAGE_BUTTON = 7;
        const int SUBCMD_DIAGRESULT = 9;
        const int SUBCMD_COMU_ERROR = 10;
        const int SUBCMD_UNEXECUTED = 12;
        const int SUBCMD_KEY_JAM = 13;
        const int SUBCMD_NO_LIGHT_PUSH = 100;
        const int SUBCMD_PRODUCT_FUNCTIONSETTING_INFO = 252;

        bool bAPIOpen, bIsCountingTest;
        byte iDigitPoint = 0, iTagMode, iCountingNum;
        short iLEDInterval, iNodeAddr;
        int GWCount, iNumData;
        int[] GWID = new int[1000];
        Diagnosis[] diagnosis = new Diagnosis[1000];

        public Abpick()
        {
            FormMain_Load();
        }
        ~Abpick()
        {
            bClose_Click();
        }
        private void FormMain_Load()
        {
            String currentpath;
            currentpath = System.IO.Directory.GetCurrentDirectory();
            ShowMsg(currentpath);

            this.timerRcv = new System.Windows.Forms.Timer();
            this.timerRcv.Tick += new System.EventHandler(this.timerRcv_Tick);
            timerRcv.Interval = 100; timerRcv.Enabled = false;

        }
        public void bOpen_Click(bool mode)
        {
            Dap_Open();
            ClearGWQuene();
            TagClear();
            GetGWStatus();

            if (mode)
            {
                timerRcv.Enabled = true;
            }
            else
            {
                threadRcv = new Thread(RecFun);
                threadRcv.IsBackground = true;
                threadRcv.Start();
            }

            WriteLog("AT500 OPEN");
        }

        public void bClose_Click()
        {
            timerRcv.Enabled = false;
            threadRcvflg = false; Thread.Sleep(500);

            TagClear();
            Dap_Close();
            WriteLog("AT500 CLOSED");

            threadRcvflg = true;
        }

        private System.Windows.Forms.Timer timerRcv;
        private void timerRcv_Tick(object sender, EventArgs e)
        {
            RcvMsg();
        }
        private void RecFun()
        {
            while (threadRcvflg)
            {
                Thread.Sleep(100);
                RcvMsg();
            }
        }



        int intpasscount = 0;
        int interrcount = 0;

        int intAB_GW_Status = -100;
        int intAB_Tag_RcvMsg = -100;

        TextBox txMsg = new TextBox();
        /// <summary>
        /// 显示信息
        /// </summary>
        /// <param name="msg"></param>
        private void ShowMsg(String msg)
        {
            if (txMsg.InvokeRequired)
            {
                ShowMsgCallback b = new ShowMsgCallback(ShowMsg);
                txMsg.Invoke(b, new object[] { msg });
            }
            else
            {
                try
                {
                    Dapapi.AB_AHA_ClrDsp(GWID[0], -86);
                    if (msg.Contains("SubCmd:6,Data:NOREAD"))
                    {
                        Dapapi.AB_Melody_On(GWID[0], -1, 1, 1);
                        Dapapi.AB_AHA_DspStr(GWID[0], -86, "   NOREAD   ", 1, 1); //1显示频率长亮

                        WriteLog(msg);
                        Thread.Sleep(500);
                        Dapapi.AB_Melody_On(GWID[0], -1, 1, 0);
                        Dapapi.AB_LB_DspNum(GWID[0], -19, ++interrcount, 0, 0);
                        Dapapi.AB_LED_Status(GWID[0], -19, 0, 1); //Red led
                    }
                    else
                    {
                        if (msg.Contains("TagNode:-84,SubCmd:6,Data:1S"))
                        {
                            string str = msg.Split(':')[5].Substring(2, 4) + msg.Split(':')[5].Substring(12, 8);
                            Dapapi.AB_AHA_DspStr(GWID[0], -86, str, 1, 1); //1显示频率长亮
                            Dapapi.AB_AHA_DspStr(GWID[0], -20, "PASS", 0, 0);
                            Dapapi.AB_LB_DspNum(GWID[0], -21, ++intpasscount, 0, 0);
                            Dapapi.AB_LED_Status(GWID[0], -21, 2, 1); //Amber led
                        }
                        else
                        {
                            Dapapi.AB_AHA_DspStr(GWID[0], -86, "    PASS    ", 1, 1); //1显示频率长亮
                        }
                        WriteLog(msg);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// 接收标签信息
        /// </summary>
        private void RcvMsg()
        {
            int gwid, ret;
            short tagNode, subCmd, msgType, dataCnt;
            short gwPort, keyType, maxTag;
            byte[] rcvData = new byte[200];
            Dapapi.Tccb ccb_data;
            string tmpStr, rcvStr;


            gwid = 0;       //all gateway  

            gwid = 1;       //my gateway ID
            tagNode = 0;    //all tagnode
            subCmd = -1;    //all subcmd
            msgType = 0;
            dataCnt = 200;

            ret = Dapapi.AB_GW_Status(gwid);  //网关ID号
            if (intAB_GW_Status != ret)
            {
                intAB_GW_Status = ret; WriteLog("AB_GW_Status(0)=" + ret);
            }

            ret = Dapapi.AB_Tag_RcvMsg(ref gwid, ref tagNode, ref subCmd, ref msgType, rcvData, ref dataCnt);
            if (intAB_Tag_RcvMsg != ret)
            {
                intAB_Tag_RcvMsg = ret; WriteLog("AB_Tag_RcvMsg()=" + ret);
            }

            if (ret > 0)
            {
                rcvStr = System.Text.Encoding.Default.GetString(rcvData);
                rcvStr = rcvStr.Replace("\0", "") + " ";

                if (tagNode < 0)
                    gwPort = 1;
                else
                    gwPort = 2;

                tmpStr = "GW_ID:" + gwid + ",GW Port:" + gwPort + ",TagNode:" + tagNode + ",SubCmd:" + subCmd + ",Data:" + rcvStr;

                switch (subCmd)
                {
                    case SUMCMD_CONFIRM_BUTTON:
                        ShowMsg(tmpStr + @"Message:Confirm key is pressed.");
                        break;
                    case SUBCMD_SHORTAGE_BUTTON:
                        ShowMsg(tmpStr + @"Message:Shortage key is pressed.");
                        break;
                    case SUBCMD_COMU_ERROR:
                        ShowMsg(tmpStr + @"Message:Communication fail (Time out).");
                        break;
                    case SUBCMD_UNEXECUTED:
                        ShowMsg(tmpStr + @"Message:Un_executed command.");
                        break;
                    case SUBCMD_KEY_JAM:
                        Dapapi.AB_LB_DspStr(gwid, tagNode, "EEE", 0, 0);

                        ShowMsg(tmpStr + @"Message:Confirmation key fault(Key jam).");
                        break;
                    case SUBCMD_NO_LIGHT_PUSH:
                        switch (msgType)
                        {
                            case 0:
                                keyType = (byte)Dapapi.AB_GW_RcvButton(rcvData, ref dataCnt);
                                tmpStr = tmpStr + @"Message:Key code return. Key type = " + keyType + " (";
                                switch (keyType)
                                {
                                    case 1:
                                        tmpStr = tmpStr + @"Only CONFIRM button)";
                                        break;
                                    case 2:
                                        tmpStr = tmpStr + @"Only UP button)";
                                        break;
                                    case 3:
                                        tmpStr = tmpStr + @"Only DOWN button)";
                                        break;
                                    case 4:
                                        tmpStr = tmpStr + @"First UP, then CONFIRM)";
                                        break;
                                    case 5:
                                        tmpStr = tmpStr + @"First DOWN, then CONFIRM)";
                                        break;
                                    case 6:
                                        tmpStr = tmpStr + "First UP and DOWN, then CONFIRM)";
                                        break;
                                    case 7:
                                        tmpStr = tmpStr + "First DOWN, then UP)";
                                        break;
                                    case 8:
                                        tmpStr = tmpStr + "First CONFIRM, then UP)";
                                        break;
                                    case 9:
                                        tmpStr = tmpStr + "First DOWN and CONFIRM, then UP)";
                                        break;
                                    case 10:
                                        tmpStr = tmpStr + "Fisrt UP, then DOWN)";
                                        break;
                                    case 11:
                                        tmpStr = tmpStr + "First CONFIRM, then DOWN)";
                                        break;
                                    case 12:
                                        tmpStr = tmpStr + "First UP and CONFIRM, then DOWN)";
                                        break;
                                    default:
                                        tmpStr = tmpStr + "UnKnow)";
                                        break;
                                }
                                break;
                            case 1:
                                ShowMsg(tmpStr + @"Message:Tag busy.");
                                break;
                            case 2:
                                ShowMsg(tmpStr + @"Message:First record disappear.");
                                break;
                            case 3:
                                ShowMsg(tmpStr + @"Message:506-3W-123 Last Record Confirmed.");
                                break;
                            case 7:
                                ShowMsg(tmpStr + @"Sensor CAPS auto warning return.");
                                break;
                        }
                        break;

                    case SUBCMD_DIAGRESULT:
                        string diagstr = "";
                        ShowMsg(tmpStr + @"DiagResult");

                        DiagResult(rcvData, ref diagstr);
                        ShowMsg("GWID:" + gwid + ",Port:" + gwPort + ",SubCmd:" + SUBCMD_DIAGRESULT + ",MaxTag:" + diagstr.Length + ",Data:" + diagstr + ",Set Best Polling Success.");
                        //ret = Dapapi.AB_GW_RcvMsg(gwid, cData);
                        //if (ret == 43)
                        //{
                        //    for (int i = 0; i < 250; i++)
                        //    {
                        //        ccbData[i] = cData[i + 8];
                        //    }
                        //    gwPort = (byte)(ccb_data.msgtype - 96 + 1);
                        //    maxTag = (short)DiagResult(ccbData, ref rcvStr);

                        //    for (int j = 1; j < GWCount; j++)
                        //    {
                        //        if (gwPort == 1)
                        //        {
                        //            ret = Dapapi.AB_GW_SetPollRang(GWID[j], maxTag);
                        //        }
                        //        else
                        //        {
                        //            ret = Dapapi.AB_GW_SetPollRang(GWID[j], (short)(maxTag * -1));
                        //        }
                        //    }

                        //    ShowMsg("GWID:" + gwid + ",Port:" + gwPort + ",SubCmd:" + cData[6] + ",MaxTag:" + maxTag + ",Data:" + rcvStr + ",Set Best Polling Success.");
                        //}
                        break;

                    case SUBCMD_PRODUCT_FUNCTIONSETTING_INFO:
                        tmpStr = "GWID:" + gwid + ",GW Port:" +
                            gwPort + ",Tag Node:" + tagNode + ",SubCmd:" + subCmd + ",Serial:" + rcvData[0] + ",Version:" + rcvData[1] + ",TagMode:" + rcvData[2];
                        ShowMsg(tmpStr);
                        break;

                    default:
                        ShowMsg(tmpStr);
                        break;
                }
            }
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="msg"></param>
        private void WriteLog(string msg)
        {
            Console.WriteLine(DateTime.Now.ToString() + " " + msg);

            String logFile = "pick.log";

            if (!File.Exists(logFile))
            {
                FileStream fs = new FileStream(logFile, FileMode.CreateNew);
                fs.Close();
            }

            using (StreamWriter sw = File.AppendText(logFile))
            {
                sw.WriteLine(DateTime.Now.ToString() + "=>" + msg);
            }
        }

        /// <summary>
        /// 解析接收诊断值
        /// </summary>
        /// <param name="ccb_data"></param>
        /// <param name="tagstr"></param>
        /// <returns></returns>
        private int DiagResult(byte[] ccb_data, ref string tagstr)
        {
            int k, tmp, maxid;

            tagstr = "";
            tmp = 0;
            maxid = 0;

            for (k = 1; k <= 250; k++)
            {
                if ((k - 1) % 8 == 0)
                {
                    tmp = ccb_data[3 + (k - 1) / 8];
                }
                if (tmp % 2 != 1)
                {
                    maxid = k;
                    tagstr = tagstr + "1";
                }
                else
                {
                    tagstr = tagstr + "0";
                }
                tmp = tmp / 2;
            }

            tagstr = tagstr.Substring(0, maxid);

            return maxid;
        }

        /// <summary>
        /// API 函数初始化
        /// </summary>
        /// <returns></returns>
        private int APIOpen()
        {
            if (!bAPIOpen)
            {
                int ret;
                try
                {
                    ret = Dapapi.AB_API_Open();
                    if (ret < 0)
                        return -1;
                    else
                    {
                        bAPIOpen = true;
                        return 1;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }
            return 0;
        }

        /// <summary>
        /// API注销
        /// </summary>
        /// <returns></returns>
        private int APIClose()
        {
            if (bAPIOpen)
            {
                Dapapi.AB_API_Close();
                bAPIOpen = false;
                return 1;
            }
            else
                return 0;
        }

        /// <summary>
        /// 控制器打开
        /// </summary>
        /// <returns></returns>
        private int Dap_Open()
        {
            int posspace, postab, pos;

            GWCount = 0;
            if (!System.IO.File.Exists("IPINDEX"))
            {
                Console.WriteLine("文件IPINDEX不存在!");
                return -1;
            }
            try
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader("IPINDEX"))
                {
                    String line;
                    // Read and display lines from the file until the end of 
                    // the file is reached.
                    while ((line = sr.ReadLine()) != null)
                    {
                        GWCount++;
                        posspace = line.IndexOf(" ");   //find space
                        postab = line.IndexOf((char)9); //find tab

                        if (posspace <= 0) posspace = postab;
                        if (postab <= 0) postab = posspace;
                        pos = System.Math.Min(posspace, postab);
                        if (pos <= 0)
                        {
                            Console.WriteLine("IPINDEX格式错误!");
                            return -1;
                        }

                        //txAddrList.AppendText(line + "\r\n");

                        GWID[GWCount - 1] = int.Parse(line.Substring(0, pos));
                        diagnosis[GWCount - 1].Init(2);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("读取文件IPINDEX失败!" + e.Message);
                return -1;
            }

            if (APIOpen() < 0)
            {
                Console.WriteLine("初始化API失败!");
                return -1;
            }

            ShowMsg("API Open Success!");

            for (int i = 0; i < GWCount; i++)
            {
                if (Dapapi.AB_GW_Open(GWID[i]) < 0)
                {
                    ShowMsg("控制器'" + GWID[i] + "'打开失败!");
                }
            }

            return 1;
        }

        /// <summary>
        /// 控制器关闭
        /// </summary>
        private void Dap_Close()
        {
            //TagClear
            APIClose();
        }

        /// <summary>
        /// 熄灭所有标签
        /// </summary>
        private void TagClear()
        {
            for (int i = 0; i < GWCount; i++)
            {
                if (Dapapi.AB_GW_Status(GWID[i]) == 7)
                {
                    Dapapi.AB_LB_DspNum(GWID[i], -252, 0, 0, -3);
                    Dapapi.AB_LB_DspNum(GWID[i], 252, 0, 0, -3);
                    Dapapi.AB_LED_Dsp(GWID[i], -252, 0, 0);
                    Dapapi.AB_LED_Dsp(GWID[i], 252, 0, 0);
                    Dapapi.AB_BUZ_On(GWID[i], -252, 0);
                    Dapapi.AB_BUZ_On(GWID[i], 252, 0);
                    Dapapi.AB_LB_DspStr(GWID[i], -252, "", 0, -3);
                    Dapapi.AB_LB_DspStr(GWID[i], 252, "", 0, -3);

                    //12-digits Alphanumerical display
                    Dapapi.AB_AHA_ClrDsp(GWID[i], -252);
                    Dapapi.AB_AHA_ClrDsp(GWID[i], 252);
                    Dapapi.AB_AHA_BUZ_On(GWID[i], -252, 0);
                    Dapapi.AB_AHA_BUZ_On(GWID[i], 252, 0);
                }
            }
        }

        /// <summary>
        /// 清空消息对列
        /// </summary>
        private void ClearGWQuene()
        {
            byte[] cData = new byte[200];

            int gwid;
            short tagNode, subCmd, msgType, dataCnt;

            gwid = 0;
            tagNode = 0;
            subCmd = -1;
            msgType = 0;
            dataCnt = 0;
            while (Dapapi.AB_Tag_RcvMsg(ref gwid, ref tagNode, ref subCmd, ref msgType, cData, ref dataCnt) > 0)
            {
            }
        }

        /// <summary>
        /// 获取控制器状态
        /// </summary>
        private void GetGWStatus()
        {
            bool bGoOn;
            int ret, timeStart;

            for (int i = 0; i < GWCount; i++)
            {
                Dapapi.AB_GW_Open(GWID[i]);
                ret = Dapapi.AB_GW_Status(GWID[i]);

                if (ret != 7)
                {
                    bGoOn = true;
                    timeStart = System.Environment.TickCount;
                    while (bGoOn)
                    {
                        ret = Dapapi.AB_GW_Status(GWID[i]);
                        if (ret == 7)
                            bGoOn = false;
                        else if (System.Environment.TickCount - timeStart > 3000)
                        {
                            bGoOn = false;
                        }
                    }
                }

                if (ret == 7)
                {
                    ShowMsg("Gateway ID:" + GWID[i] + " success connected, status return :" + ret);
                }
                else
                {
                    ShowMsg("Gateway ID:" + GWID[i] + " failed to connected, status return :" + ret);
                }

            }
        }

        /// <summary>
        /// 获取连结错误代码
        /// </summary>
        /// <param name="retcode"></param>
        /// <returns></returns>
        private string GetConnectErr(int retcode)
        {
            switch (retcode)
            {
                case -3:
                    return "Parameter data is error!";
                case -2:
                    return "TCP is not created yet!";
                case -1:
                    return "DAP_IP out of range !";
                case 0:
                    return "Closed";
                case 1:
                    return "Open";
                case 2:
                    return "Listening";
                case 3:
                    return "Connection is Pending";
                case 4:
                    return "Resolving the host name";
                case 5:
                    return "Host is resolved";
                case 6:
                    return "Waiting to Connect";
                case 7:
                    return "Connect OK";
                case 8:
                    return "Connection is closing";
                case 9:
                    return "State error has occurred";
                case 10:
                    return "Connection state is undetermined";
                default:
                    return "Unkown error code";
            }
        }

        /// <summary>
        /// 检查字符是否是数字
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool IsNumber(String value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (!Char.IsDigit(value, i))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
