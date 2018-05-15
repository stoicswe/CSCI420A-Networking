using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebChatServer
{
    class Frame
    {
        bool fin;
        bool rsv1;
        bool rsv2;
        bool rsv3;
        byte opcode;
        byte[] mask;
        Int64 payloadLength;
        public byte[] payload;

        private Frame()
        {

        }

        private static void print(Object any)
        {
            Console.WriteLine(any);
        }

        static byte[] ConcatAll(IEnumerable<byte[]> bytes)
        {
            var bs = bytes.ToList();
            var l = (from b in bs
                     select b.Length).Sum();
            int i = 0;
            var result = new byte[l];
            foreach (var b in bs)
            {
                b.CopyTo(result, i);
                i += b.Length;
            }
            return result;
        }

        public static byte[] Build(string message)
        {
            var a = new byte[2];
            a[0] = 1;
            if (message.Length < 126)
            {
                a[1] = (byte) Convert.ToInt16(message.Length);
            } 
            else 
            {
                a[1] = (byte) 126;
            }
            var bits = new BitArray(a);
            //bits[15] = true;
            bits[7] = true;
            bits.CopyTo(a, 0);
            //var mask = new byte[4];
            var payload = Encoding.UTF8.GetBytes(message);
            var kiddoFrame = ConcatAll(new byte[][]{ a, payload });
            var teenageFrame = ReadFrame(new BinaryReader(new MemoryStream(kiddoFrame)));
            
            return kiddoFrame;
        }

        public static Frame ReadFrame(BinaryReader s)
        {
            byte[] a = s.ReadBytes(2);
            BitArray bits = new BitArray(a);
            var f = new Frame();
            f.fin = bits[15];
            f.rsv1 = bits[14];
            f.rsv2 = bits[13];
            f.rsv3 = bits[12];
            f.opcode = (byte)(a[0] & 7);
            var mb = bits[7];
            var len = a[1] & 127;
            f.payloadLength = (long) ReadLen(s, len);
            //print(f.payloadLength);
            if (mb)
            {
                f.mask = s.ReadBytes(4);
            }
            f.payload = s.ReadBytes(Convert.ToInt32(f.payloadLength));
            if(mb & 1 == 1)
            {
                UnMask(f.mask, f.payload);
            }
            print($"{f.fin}:{f.rsv1}:{f.rsv2}:{f.rsv3}:{f.opcode}:{mb}");
            //print($"{f.mask}");
            return f;
        }

        private static UInt64 ReadLen(BinaryReader b, int len)
        {
            if (len == 126) return (UInt64) b.ReadUInt16();
            if (len == 127) return (UInt64) b.ReadUInt64();
            return (UInt64) len;
        }

        private static void UnMask(byte[] mask, byte[] payload)
        {
            for(var i = 0; i < payload.Length; i++)
            {
                payload[i] = (byte) (payload[i] ^ mask[i % 4]);
            }
        }

    }
}
