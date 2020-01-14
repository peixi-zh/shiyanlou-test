using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace 包装后段测试
{
    class error_alarm
    {
        public static void printscreen()  //报警声音，抓取当前屏幕，并且保存图片文件
        {
            Console.WriteLine("检查到PROM有错误!");
            try
            {
                new System.Media.SoundPlayer("cuo.wav").Play();
            }
            catch { }   //防止不存在音频文件

            //获得当前屏幕的大小   //Catch screen
            Rectangle rect = new Rectangle();
            rect = System.Windows.Forms.Screen.GetWorkingArea(rect);   //工作区全屏抓取，除了任务栏
            int W = rect.Width, H = rect.Height;
            Size mySize = new Size(W, H);        //图片尺寸定义
            Bitmap bitmap = new Bitmap(rect.Width, rect.Height);
            bitmap = new Bitmap(W, H);
            Graphics g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(0, 0, 0, 0, mySize);
            bitmap.Save(DateTime.Now.ToString("yyyyMMddHHmmss") + ".JPG");

            //释放资源  
            bitmap.Dispose();
            g.Dispose();
            GC.Collect();
        }
    }
}
