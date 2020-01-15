using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Apiabpick
{
    class Dapapi
    {
        [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 258)]
        public struct Tccb
        {
            public short len;
            public byte msgtype;
            public byte dstnote;
            public byte srcnote;
            public byte cmdtype;
            public byte sumcmd;
            public byte subnode;
            public byte[] data;
        }
        [DllImport("ProTCPUDP.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int INITClient(StringBuilder ip, int port);

        [DllImport("ProTCPUDP.dll")]
        public static extern int DiscnnSrv(StringBuilder ip, int port);

        /// <summary>
        /// Startup and initialize the ABLEPick DAPAPI.DLL API function cal
        /// ,Example for file: "ipindex
        /// </summary>
        /// <returns>大于0 -> OK, 小于等于0 -> error</returns>
        [DllImport("Dapapi.dll")]
        public static extern int AB_API_Open();

        /// <summary>
        /// Shutdown the ABLEPICK API.
        /// </summary>
        /// <returns>0 -> OK, -1 -> error</returns>
        [DllImport("Dapapi.dll")]
        public static extern int AB_API_Close();
        /// <summary>
        /// To build up a TCP connection with the specified TCP/IP controller. Normally, the program usually will
        ///  Function AB_GW_Status to check about the connection status. Please refer the description of AB_GW_Status()
        /// </summary>
        /// <param name="Gateway_ID">0 -> current Gateway.>0 -> the specified Gateway.</param>
        /// <returns></returns>
        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_Open(int Gateway_ID);

        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_Close(int Gateway_ID);

        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_Cnt();

        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_Conf(int Ndx, int Gateway_ID, Byte Ip, int Port);

        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_Ndx2ID(int Ndx);

        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_ID2Ndx(int Gateway_ID);

        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_InsConf(int Gateway_ID, Byte Ip, int Port);

        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_UpdConf(int Gateway_ID, Byte Ip, int Port);

        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_DelConf(int Gateway_ID);

        //Get Gateway Status
        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_Status(int Gateway_ID);
        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_AllStatus(ref Byte Status);
        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_TagDiag(int ateway_ID, int Port_ID);

        // Get/Send message from/to Gateway
        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_RcvMsg(int Gateway_ID, byte[] cdata);
        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_SndMsg(int Gateway_ID, StringBuilder Data);
        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_RcvReady(int Gateway_ID);
        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_RcvButton(byte[] Data, ref short Data_Cnt);
        [DllImport("Dapapi.dll")]
        public static extern int AB_GW_SetPollRang(int Gateway_ID, short Node_Range);

        //Send Message to picking tag
        [DllImport("Dapapi.dll")]
        public static extern int AB_Tag_RcvMsg(ref int Gateway_ID, ref short Node_Addr, ref short Subcmd, ref short Msg_Type, byte[] Data, ref short Data_Cnt);
        [DllImport("Dapapi.dll")]
        public static extern int AB_Tag_ChgAddr(int Gateway_ID, short Node_Addr, short New_Tag);
        [DllImport("Dapapi.dll")]
        public static extern int AB_BUZ_On(int Gateway_ID, short Node_Addr, Byte Buzzer_Type);
        [DllImport("Dapapi.dll")]
        public static extern int AB_LB_DspNum(int Gateway_ID, short Node_Addr, int Dsp_Int, Byte Dot, short Interval);
        [DllImport("Dapapi.dll")]
        public static extern int AB_LB_DspStr(int Gateway_ID, short Node_Addr, String Dsp_Str, Byte Dot, short Interval);
        [DllImport("Dapapi.dll")]
        public static extern int AB_LB_SetMode(int Gateway_ID, short Node_Addr, Byte Pick_Mode);
        [DllImport("Dapapi.dll")]
        public static extern int AB_LB_Simulate(int Gateway_ID, short Node_Addr, Byte Simulate_Mode);
        [DllImport("Dapapi.dll")]
        public static extern int AB_LB_SetLock(int Gateway_ID, short Node_Addr, Byte Lock_State, Byte Lock_Key);
        [DllImport("Dapapi.dll")]
        public static extern int AB_All_Simple(int Gateway_ID, Byte Lamp_STA, Byte Gw_Port);
        [DllImport("Dapapi.dll")]
        public static extern int AB_LB_DspAddr(int Gateway_ID, short Node_Addr);
        [DllImport("Dapapi.dll")]
        public static extern int AB_TAG_Reset(int Gateway_ID, short Node_Addr);
        [DllImport("Dapapi.dll")]
        public static extern int AB_TAG_mode(int Gateway_ID, short Node_Addr, short Save_Mode, Byte Mode_Type);
        [DllImport("Dapapi.dll")]
        public static extern int AB_TAG_CountDigit(int Gateway_ID, short Node_Addr, short CountDigit);
        [DllImport("Dapapi.dll")]
        public static extern int AB_LED_Dsp(int Gateway_ID, short Node_Addr, Byte Lamp_STA, Byte Interval);
        [DllImport("Dapapi.dll")]
        public static extern int AB_LED_Status(int Gateway_ID, short Node_Addr, Byte Lamp_Color, Byte Lamp_STA);
        [DllImport("Dapapi.dll")]
        public static extern int AB_TAG_Complete(int Gateway_ID, short Node_Addr, Byte Complete_STA);
        [DllImport("Dapapi.dll")]
        public static extern int AB_TAG_ButtonDelay(int Gateway_ID, short Node_Addr, Byte Delay_Time);

        // 12-digits A;phanumerical display
        [DllImport("Dapapi.dll")]
        public static extern int AB_AHA_DspStr(int Gateway_ID, short Node_Addr, String Disp_Str, Byte BeConfirm, Byte DigitSta);
        [DllImport("Dapapi.dll")]
        public static extern int AB_AHA_ClrDsp(int Gateway_ID, short Node_Addr);
        [DllImport("Dapapi.dll")]
        public static extern int AB_AHA_ReDsp(int Gateway_ID, short Node_Addr);
        [DllImport("Dapapi.dll")]
        public static extern int AB_AHA_LED_Dsp(int Gateway_ID, short Node_Addr, Byte Lamp_Type, Byte Lamp_STA);
        [DllImport("Dapapi.dll")]
        public static extern int AB_AHA_BUZ_On(int Gateway_ID, short Node_Addr, short Buzzer_Type);

        // 3-digit directional and 2-digit vertical & directional pick  tag(AT503A&AT502V) 
        [DllImport("Dapapi.dll")]
        public static extern int AB_AV_LB_DspNum(int Gateway_ID, short Node_Addr, int Disp_Data, Byte Arrow, Byte Dot, short Interval);

        // 6-digit,3 separated windows pick tag(AT506-3W-123)
        [DllImport("Dapapi.dll")]
        public static extern int AB_3W_LB_DspNum(int Gateway_ID, short Node_Addr, String Row, String Col, int Disp_INT, Byte Dot, int Interval);

        // Melody completion indicator (AT510M)
        [DllImport("Dapapi.dll")]
        public static extern int AB_Melody_On(int Gateway_ID, short Node_Addr, Byte Song, Byte Buzzer_Type);
        [DllImport("Dapapi.dll")]
        public static extern int AB_Melody_Volume(int Gateway_ID, short Node_Addr, Byte Volume);

        // Cable-less picking tag set up node address automatically.
        [DllImport("Dapapi.dll")]
        public static extern int AB_CLTAG_DspAddr(int Gateway_ID, short Port, Byte Mode);

        [DllImport("Dapapi.dll")]
        public static extern int AB_CLTAG_SetAddr(int Gateway_ID, short Node_Addr, Byte Set_Mode);


        //AT702S
        [DllImport("Dapapi.dll")]
        public static extern int AB_SNR_AutoWarn (int Gateway_ID, int Node_addr, int AutoWarn);
        [DllImport("Dapapi.dll")]
        public static extern int AB_SNR_SetRange (int Gateway_ID, int Node_addr, byte WorkingRange , byte savemode);
        [DllImport("Dapapi.dll")]
        public static extern int AB_SNR_AutoRange (int Gateway_ID, int Node_addr);
        [DllImport("Dapapi.dll")]
        public static extern int AB_SNR_ResidualDSP (int Gateway_ID, int Node_addr, byte nResidual);
        [DllImport("Dapapi.dll")]
        public static extern int AB_SNR_Control (int Gateway_ID, int Node_addr, byte ncontrol, byte savemode);
        [DllImport("Dapapi.dll")]
        public static extern int AB_SNR_DetectTime (int Gateway_ID, int Node_addr, byte DetectTime, byte savemode);

    }
}
