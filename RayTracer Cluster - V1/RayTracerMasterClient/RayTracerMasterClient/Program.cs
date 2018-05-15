using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;


public class RayTracerMasterClient
{

    public static void Main(string[] args)
    {

        try
        {
            TcpClient tcpclnt = new TcpClient();
            Console.WriteLine("Connecting.....");

            tcpclnt.Connect("localhost", 199712);//put here the server ip

            Console.WriteLine("Connected");
            StreamReader sceneData = new StreamReader(args[0]);
            string scene = sceneData.ReadToEnd();
            Stream stm = tcpclnt.GetStream();

            ASCIIEncoding asen = new ASCIIEncoding();
            byte[] ba = asen.GetBytes(scene);
            Console.WriteLine("Transmitting.....");

            stm.Write(ba, 0, ba.Length);

            byte[] bb = new byte[1000000000];
            int k = stm.Read(bb, 0, 1000000000);

            StreamWriter render = new StreamWriter(File.OpenWrite("final.ppm"));
            for (int i = 0; i < k; i++)
                render.Write(Convert.ToChar(bb[i]));

            tcpclnt.Close();
        }

        catch (Exception e)
        {
            Console.WriteLine("Error..... " + e.StackTrace);
        }
    }

}
