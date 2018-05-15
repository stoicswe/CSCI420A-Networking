using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace WebChatServer
{
    class Client
    {
        BlockingCollection<Message> messageStore;
        private TcpClient clientConnection;
        private NetworkStream dataStream;
        private bool verbose = false;
        private bool messages = false;

        public Client(TcpClient clientConnection, NetworkStream dataStream, BlockingCollection<Message> messageStore, bool verbose = true)
        {
            this.dataStream = dataStream;
            this.clientConnection = clientConnection;
            this.verbose = verbose;
            this.messageStore = messageStore;
        }

        public void Run()
        {
            using (var br = new BinaryReader(dataStream))
            {
                while (true)
                {
                    Frame f;
                    f = Frame.ReadFrame(br);
                    if (verbose) {Console.WriteLine(Encoding.UTF8.GetString(f.payload));}
                    var d = Encoding.UTF8.GetString(f.payload).Split("::");
                    if (d.Length > 2)
                    {
                        if (verbose) { Console.WriteLine("{0} {1} Message recieved, queuing contents for handle {2} {3} {4}", Program.globalAccumulator.getValue(), this, d[0], d[1], d[2]); }
                        messageStore.Add(new Message(this, d[0], d[1], FriendlyIp(clientConnection.Client.RemoteEndPoint) + " " + d[2]));
                        messages = true;
                    }
                    else
                    {
                        if (verbose) { Console.WriteLine("{0} Client sent bad message...ignoring...", this); }
                    }
                }
            }
        }

        public string FriendlyIp(EndPoint me)
        {
            if (me is IPEndPoint i) 
            {
                return i.Address.ToString();
            }
            return me.ToString();
        }

        public void Send(Message m)
        {
            if (m != null)
            {
                if(verbose) {Console.WriteLine("{0} {1} Sending data...", Program.globalAccumulator.getValue(), this);}
                var ms = $"{m.GetSender}::{m.GetReceiver}::{m.GetMessage}";
                var sf = Frame.Build(ms);
                if (verbose) {Console.WriteLine("{0} {1} Frame Built {2}", Program.globalAccumulator.getValue(), this, sf);}
                dataStream.Write(sf, 0, sf.Length);
            } else
            {
                if (verbose) { Console.WriteLine("{0} ERROR: Bad null message. Ignoring....", this); }
            }
        }

        public Message Receive()
        {
            Message t = null;
            if (messageStore.Count > 0 && messages)
            {
                t = messageStore.Take();
                if (messageStore.Count == 0)
                {
                    messages = false;
                }
                if (t != null)
                {
                    if (verbose) { Console.WriteLine("{0} {1} Sending message back to the server handle {2} {3} {4}", Program.globalAccumulator.getValue(), this, t.GetSender, t.GetReceiver, t.GetMessage); }
                }
                else
                {
                    if (verbose) { Console.WriteLine("{0} {1} This is a blank request for a message, received another request?", Program.globalAccumulator.getValue(), this); }
                }
            }
            return t;
        }

        public bool Alive()
        {
            return clientConnection.Connected;
        }

        public static string GetDecodedData(byte[] buffer, int length)
        {
            byte b = buffer[1];
            int dataLength = 0;
            int totalLength = 0;
            int keyIndex = 0;

            if (b - 128 <= 125)
            {
                dataLength = b - 128;
                keyIndex = 2;
                totalLength = dataLength + 6;
            }

            if (b - 128 == 126)
            {
                dataLength = BitConverter.ToInt16(new byte[] { buffer[3], buffer[2] }, 0);
                keyIndex = 4;
                totalLength = dataLength + 8;
            }

            if (b - 128 == 127)
            {
                dataLength = (int)BitConverter.ToInt64(new byte[] { buffer[9], buffer[8], buffer[7], buffer[6], buffer[5], buffer[4], buffer[3], buffer[2] }, 0);
                keyIndex = 10;
                totalLength = dataLength + 14;
            }

            if (totalLength > length)
                throw new Exception("The buffer length is small than the data length");

            byte[] key = new byte[] { buffer[keyIndex], buffer[keyIndex + 1], buffer[keyIndex + 2], buffer[keyIndex + 3] };

            int dataIndex = keyIndex + 4;
            int count = 0;
            for (int i = dataIndex; i < totalLength; i++)
            {
                buffer[i] = (byte)(buffer[i] ^ key[count % 4]);
                count++;
            }

            return Encoding.ASCII.GetString(buffer, dataIndex, dataLength);
        }
    }
}
