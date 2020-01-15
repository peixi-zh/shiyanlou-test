using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace 包装队列显示
{
    public partial class Frm : Form
    {
        public Frm()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;     //允许线程访问UI控件
        }

        const int BURFERSIZE = 128;     //接受的数据缓冲区大小

        private void Frm_Load(object sender, EventArgs e)
        {
            string strmode = File.ReadAllText("mode.txt", Encoding.Default);
            if (strmode.Equals("1"))
            {
                button1.Text = "单";
            }
            else
            {
                button1.Text = "双";
            }            
            Thread DBThreadArray = new Thread(showqueue_title);    //循环查询队列和显示
            DBThreadArray.IsBackground = true;
            DBThreadArray.Start();            
        }
              
        static bool UapdateMySqlteautouwip(string tabname, string uwipmtsn)         //更新PROM打单或发送贴单机器人完成时间信息
        {
            MySqlConnection conn = new MySqlConnection(MySqlStringteauto);
            string SqlCmdString = "";
            SqlCmdString = string.Format("UPDATE {0} SET DTimeSecond = '{1}',DTimeThird = '{1}',DTimeFourth = '{1}',BatterySN = '{1}',Storage = '{1}' WHERE MTSN = '{2}'", tabname, "D", uwipmtsn);
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
                    Console.WriteLine("更新失败");
                    File.AppendAllText("UapdateMySqlteautouwip.log", "\r\n更新DTimeThird 标记失败!,指令是:" + uwipmtsn + ", " + DateTime.Now.ToString(), Encoding.Default);
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
        static bool lockflg = false;                
        
        private void showqueue_title()
        {
            int i = 0;
            while (true)
            {
                listBox1.Items.Clear();
                showqueueonce_DTimenull();
                Thread.Sleep(2000);
                if (++i > 100)   //约3min
                {
                    Autoit.AU3_MouseMove(800, 745, 100);   //数值越大移动越慢
                    Thread.Sleep(1000); i = 0;
                    Autoit.AU3_MouseMove(885, 745, 50);
                }
                btnUnlock.Enabled = lockflg;     //解除报警使能跟随系统卡的状态
            }
        }
        
        public static string MySqlStringteauto = @"Server=10.186.204.64;Database=teauto;Uid=wanshan;Pwd=2ASRIYhIl$E;Old Guids=true";    //infosmart

        bool showqueueonce_DTimenull()    //查uwip信息校验扫描
        {
            MySqlConnection conn = new MySqlConnection(MySqlStringteauto);
            string SqlCmdString = string.Format("SELECT * FROM {0} WHERE DTimeFourth IS NULL", "teauto.autoscanlabel");
            try   //DB Query
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(SqlCmdString, conn);
                MySqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    listBox1.Items.Add(rd[1].ToString() + "," + rd[3].ToString() + "," + rd[5].ToString() + "," + rd[6].ToString());
                }
                return true;
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

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text.Equals("单"))
            {
                button1.Text = "双";
                File.WriteAllText("mode.txt", "2", Encoding.Default);
            }
            else if (button1.Text.Equals("双"))
            {
                button1.Text = "单";
                File.WriteAllText("mode.txt", "1", Encoding.Default);
            }
        }        

        private void btnClearOne_Click(object sender, EventArgs e)
        {
            btnClearOne.Enabled = false;
            try
            {
                //移除头一条记录
                string uwipmtsn = listBox1.Items[0].ToString();
                uwipmtsn = uwipmtsn.Split(',')[0];
                if (UapdateMySqlteautouwip("autoscanlabel", uwipmtsn))
                {
                    //MessageBox.Show(uwipmtsn + "移除队列成功!", "成功");
                }
                else
                {
                    //MessageBox.Show(uwipmtsn + "移除队列失败!!!", "失败");
                }

                listBox1.Items.Clear();
                showqueueonce_DTimenull();
            }
            catch
            {
            }


            Thread.Sleep(500);
            btnClearOne.Enabled = true;
        }

        private void ChkAT500_CheckedChanged(object sender, EventArgs e)
        {
            ChkAT500.Enabled = false;            
            Thread.Sleep(2000);
            ChkAT500.Enabled = true;
        }

        private void btnUnlock_Click(object sender, EventArgs e)
        {
            lockflg = false;
        }
    }
}
