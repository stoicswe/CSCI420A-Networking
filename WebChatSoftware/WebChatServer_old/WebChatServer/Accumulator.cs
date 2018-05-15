using System;
using System.Collections.Generic;
using System.Text;

namespace WebChatServer
{
    class Accumulator
    {
        private int value = 0;
        public override string ToString()
        {
            value++;
            return value.ToString() + ": ";
        }
        public int getValue()
        {
            value++;
            return value;
        }
    }
}
