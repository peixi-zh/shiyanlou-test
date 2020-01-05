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
            one,
            two,
            three
        }
        public enum LabelResult 
        {
            normal,
            noread_others,
            fail
        }
    }
}
