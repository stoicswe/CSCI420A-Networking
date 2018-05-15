using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
//using System.Net;
using RayTracer;
using System.Diagnostics;

namespace RayTracerServer
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            try
            {
                IPAddress ipAd = IPAddress.Parse("172.21.5.99"); //get ip from comp
                TcpListener myList = new TcpListener(ipAd, 199712);
                myList.Start();

                Console.WriteLine("The server is running at port 199712...");
                Console.WriteLine("The local End point is  :" + myList.LocalEndpoint);
                Console.WriteLine("Waiting for a connection.....");

                Socket s = myList.AcceptSocket();
                Console.WriteLine("Connection accepted from " + s.RemoteEndPoint);

                byte[] sceneData = new byte[1000000000];
                int k = s.Receive(sceneData);
                Console.WriteLine("Recieved...");
                StreamWriter sceneWrtier = new StreamWriter("render.scene");
                for (int i = 0; i < k; i++)
                {
                    sceneWrtier.Write(Convert.ToChar(sceneData[i]));
                    Console.Write(Convert.ToChar(sceneData[i]));
                }
                var w = new Stopwatch();
                Console.WriteLine("Starting the render...");
                w.Start();
                RayTracerApp.Run("render.scene", "outfile.ppm");
                ASCIIEncoding asen = new ASCIIEncoding();
                string text;
                var streamReader = new StreamReader(@"outfile.ppm");
                w.Stop();
                Console.WriteLine($"Render time: {w.Elapsed}");
                text = streamReader.ReadToEnd();
                s.Send(asen.GetBytes(text));
                Console.WriteLine("\nSent render data.");
                /* clean up */
                s.Close();
                myList.Stop();

            }
            catch (Exception e)
            {
                Console.WriteLine("Error..... " + e.StackTrace);
            }
        }
    }
}