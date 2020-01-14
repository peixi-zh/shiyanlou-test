using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace 包装后段测试
{
    class select_or_send
    {

        public static void selectqueue_tomesOruwip(object param)  //prom交互线程，异步工作状态  带参数
        {
            while (true)
            {
                string uwip = "", materialnumber = "", cmdinfo = "";
                if (selectqueue_toprint(ref uwip, ref materialnumber, ref cmdinfo))   //智能打印缓存1张(遇到overpack),其它缓存2张
                {
                    if (materialnumber != null)
                    {
                        if (sendmes(uwip))      //prom or lemes
                        {
                            UapdateMySqlteauto("teauto.autoscanlabel", "ROBOT");
                            Thread.Sleep(2500);
                        }
                        else
                        {
                            UapdateMySqlteauto("teauto.autoscanlabel", "ROBOTCANCEL");
                            Thread.Sleep(1500);
                        }
                    }
                    else
                    {
                        Thread.Sleep(500);
                    }
                }
                else
                {
                    Thread.Sleep(500); //Console.WriteLine("休息一会儿!");
                }
            }
        }


        static bool selectqueue_toprint(ref string uwip, ref string materialnumber, ref string cmdinfo)//查贴单标记智能缓存队列如果是overpack则缓存1台，否则缓冲2台
        {
            MySqlConnection conn = new MySqlConnection(Result_back.value_define.MySqlStringteauto);
            string SqlCmdString = string.Format("SELECT MTSN,CartonLabel,DTimeThird FROM {0} WHERE CartonLabel IS NOT NULL AND  DTimeFourth IS NULL LIMIT 2", "teauto.autoscanlabel");
            try   //DB Query
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SqlCmdString, conn);
                MySqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    if (rd["CartonLabel"].ToString().Equals("5"))       //遇到overpack双包机型
                    {
                        if (rd["DTimeThird"].ToString().Equals("PRINT"))
                        {
                            uwip = rd[0].ToString();
                            materialnumber = rd[1].ToString();
                            cmdinfo = rd[2].ToString();
                            return true;
                        }
                        else if (rd["DTimeThird"].ToString().Equals("ROBOTCANCEL"))   //遇到送检等PROM错误未能打出标签的情况
                        {
                            continue;    //取下一条
                        }
                        else
                        {
                            break;   //return false;   //等价
                        }
                    }
                    else
                    {
                        if (rd["DTimeThird"].ToString().Equals("PRINT"))
                        {
                            uwip = rd["MTSN"].ToString();
                            materialnumber = rd["CartonLabel"].ToString();
                            cmdinfo = rd["DTimeThird"].ToString();
                            return true;
                        }
                        else
                        {
                            continue;    //取下一条
                        }
                    }
                }

                return false;
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
        }//查队列标签去发送给mes打印


        static bool sendmes(string strdata)   //mes处理
        {
            #region PROM
            int delaytime = 200;   //发送延时ms       
            if ((Autoit.AU3_WinExists("系统错误信息", "")) == 1)
            {

                error_alarm.printscreen();//报警声音，抓取当前屏
                SendKeys.SendWait("{F12}"); Thread.Sleep(delaytime);
                SendKeys.SendWait("%{F1}"); Thread.Sleep(delaytime);
                Autoit.AU3_Send("\n", 1);
                return false;
            }
            Thread.Sleep(delaytime);
            Result_back.value_define.Dt = DateTime.Now;
            Console.WriteLine("Waiting for PROM is Ready!");
            while ((Result_back.value_define.ProcessID = Autoit.AU3_WinGetState("Pro-Make系统", "")) != 15)   //激活窗口返回15,后台存在返回7,不存在返回0
            {
                if (Result_back.value_define.ProcessID == 47)        //激活47,后台39，不存在0 
                {
                    break;
                }
                else
                {
                    Thread.Sleep(delaytime); Autoit.AU3_WinActivate("Pro-Make系统", "");       //使之窗口激活最前状态，无返回值
                    Console.WriteLine("Result_back.value_define.ProcessID=" + Result_back.value_define.ProcessID);

                    #region 方式一利用循环计算超时
                    Result_back.value_define.Ts = DateTime.Now.Subtract(Result_back.value_define.Dt);

                    if (Result_back.value_define.Ts.TotalSeconds <= 20)
                    {
                        Console.Write("."); Thread.Sleep(delaytime);
                    }
                    else if (Result_back.value_define.Ts.TotalSeconds > 20)
                    {
                        Console.WriteLine("Waiting for PROM is Ready Timeout,Skip to countinue , DateTime总共花费{0}ms.", Result_back.value_define.Ts.TotalMilliseconds);
                        return false;
                    }
                    #endregion
                }
            }

            Thread.Sleep(delaytime);
            if (strdata.Length == 20 && strdata.Substring(0, 2).ToUpper() == "1S" && strdata.Substring(12, 1).ToUpper() == "P")
            {
                Autoit.AU3_WinWaitActive("Pro-Make系统", "", 6);  //等待激活，限6s  ，返回值1成功，0失败
            }
            Thread.Sleep(delaytime);
            Autoit.AU3_Send(strdata + "\r\n", 1); Thread.Sleep(delaytime * 4);

            if ((Result_back.value_define.ProcessID = Autoit.AU3_WinExists("系统错误信息", "")) > 0)
            {
                error_alarm.printscreen();//报警声音，抓取当前屏幕，并且保存文件，可以自动发送F12取消报错,延时100ms
                SendKeys.SendWait("{F12}"); Thread.Sleep(delaytime);
                SendKeys.SendWait("%{F1}"); Thread.Sleep(delaytime);
                Autoit.AU3_Send("\n", 1);
                return false;
            }
            Thread.Sleep(delaytime);
            Result_back.value_define.Dt = DateTime.Now; Console.WriteLine("Waiting for PROM finished");
            while ((Result_back.value_define.ProcessID = Autoit.AU3_WinExists("系统成功信息", "")) != 1)
            {
                Thread.Sleep(delaytime); Application.DoEvents(); Console.Write(".");
                Result_back.value_define.Ts = DateTime.Now.Subtract(Result_back.value_define.Dt);
                if (Result_back.value_define.Ts.TotalSeconds > 50)   //系统卡的处理
                {
                    Result_back.value_define.Lockflg = true;
                    //是否加入报警,待验证效果
                    //skttiebiaoscan.Skt.Send(Encoding.UTF8.GetBytes("NB\r\n"));  //NB , 缺纸
                    Console.WriteLine("侦测到PROM系统卡，无响应!");
                    while (Result_back.value_define.Lockflg)
                    {
                        if ((Result_back.value_define.ProcessID = Autoit.AU3_WinExists("系统成功信息", "")) != 1)
                        {
                            try
                            {
                                new System.Media.SoundPlayer("cuo.wav").Play();
                            }
                            catch { }   //防止不存在音频文件
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            Result_back.value_define.Lockflg = false;
                            return true;
                        }
                    }

                    return false;
                }
            }
            return true;
            #endregion
        }//mes处理


        public static bool UapdateMySqlteauto(string tabname, string cmdinfo)         //更新PROM打单或发送贴单机器人完成时间信息
        {
            MySqlConnection conn = new MySqlConnection(Result_back.value_define.MySqlStringteauto);
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
                SqlCmdString = string.Format("UPDATE {0} SET DTimeThird = '{1}' WHERE CartonLabel IS NOT NULL AND DTimeThird is null LIMIT 1", tabname, cmdinfo);
            }
            else if (cmdinfo.Equals("ROBOT"))   //发送贴标
            {
                SqlCmdString = string.Format("UPDATE {0} SET DTimeThird = '{1}' WHERE CartonLabel IS NOT NULL AND DTimeThird is null LIMIT 1", tabname, DateTime.Now.ToString("yyyyMMddHHmmss"));
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


        public static bool UapdateMySqlteauto(string tabname, string cmdinfo, string MO)         //更新预加工反向计数信息
        {
            MySqlConnection conn = new MySqlConnection(Result_back.value_define.MySqlStringteauto);
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
    }
}
