using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RayTracerServer
{
    class MainClass
    {

        static int port = 9097;
        static UdpClient sender = new UdpClient();
        static UdpClient receiver = new UdpClient(port);
        static ConcurrentQueue<string> channel = new ConcurrentQueue<string>();
        static Thread receiveProcessor = new Thread(() => receive());
        static string hostClient = "D09097";

        public static void send(string data, string host)
        {
            Console.WriteLine("Sending data to {0}", host);
            Byte[] sendBytes = Encoding.UTF8.GetBytes(data);
            sender.Send(sendBytes, sendBytes.Length, host, port);
        }

        public static void receive()
        {
            Console.WriteLine("Starting the listener...");
            while (true)
            {
                IPEndPoint pairingUtility = new IPEndPoint(IPAddress.Any, port);
                Byte[] recievedBytes = receiver.Receive(ref pairingUtility);
                string recievedData = Encoding.UTF8.GetString(recievedBytes);
                channel.Enqueue(pairingUtility.Address + " " + recievedData);
            }
        }

        public static void sort(System.Collections.Generic.List<string> lines){
            
        }

        public static void Main(string[] args)
        {
            string recievedData;
            string[] received;
            int sceneLength = -1;
            int lineNum;
            System.Collections.Generic.List<string> scene = new System.Collections.Generic.List<string>();
            int count = 0;
            string sceneFile = "recieved.scene";
            //string home = Directory.GetCurrentDirectory();
            StreamWriter sceneWriter = new StreamWriter(sceneFile);

            IPHostEntry hostEntry;

            hostEntry = Dns.GetHostEntry(hostClient);

            if (hostEntry.AddressList.Length > 0)
            {
                var ip = hostEntry.AddressList[0];
            }

            receiveProcessor.Start(); //start checking for incoming data

            while (true)
            {
                Console.WriteLine("Ready for instruction, waiting...");
                while (channel.IsEmpty)
                {
                    sort(scene);
                }

                if (channel.TryDequeue(out recievedData))
                {
                    received = recievedData.Split(':');
                    Console.WriteLine();
                    Console.WriteLine("Data recieved, interpreting...");
                    if(received[1].Equals("scene")){
                        if(int.TryParse(received[2], out sceneLength)){
                            Console.WriteLine("Scene length of {0} to be recieved.", sceneLength);
                        }

                        if (int.TryParse(received[3], out lineNum)){
                            scene.Add(lineNum + ":" + received[4]);
                            count++;
                            Console.WriteLine("Line recieved...adding line: " + lineNum);
                        }
                    }
                }

                if(count.Equals(sceneLength)){
                    Console.WriteLine("All data recieved, begin writing data...");
                    break;
                }
            }

            foreach(string line in scene){
                Console.WriteLine("Writing data [{0}]", line);
                sceneWriter.WriteLine(line);
            }

            sceneWriter.Close();
            Console.WriteLine("File written.");
        }
    }
}
