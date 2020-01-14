using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace 包装后段测试
{
    class Result_back
    {
        public enum LabelNumber
        {
            pre_labeling,
            no_labeling,
            overpack,
            one = 1,
            two = 2,
            three = 3
        }
        public enum LabelResult
        {
            normal,
            noread_others,
            fail
        }
        public struct value_define
        {
            static int processID;
            static DateTime dt;
            static TimeSpan ts;
            static bool lockflg;
            private static string mySqlStringteauto = @"Server=10.186.204.64;Database=teauto;Uid=wanshan;Pwd=2ASRIYhIl$E;Old Guids=true";

            public static int ProcessID
            {
                get
                {
                    return processID;
                }

                set
                {
                    processID = value;
                }
            }

            public static DateTime Dt
            {
                get
                {
                    return dt;
                }

                set
                {
                    dt = value;
                }
            }

            public static TimeSpan Ts
            {
                get
                {
                    return ts;
                }

                set
                {
                    ts = value;
                }
            }

            public static bool Lockflg
            {
                get
                {
                    return lockflg;
                }

                set
                {
                    lockflg = value;
                }
            }

            public static string MySqlStringteauto
            {
                get
                {
                    return mySqlStringteauto;
                }

                set
                {
                    mySqlStringteauto = value;
                }
            }
        }
    }
}
