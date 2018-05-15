using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
/*
* ============= RayTracer Client ===========
* Author: Nathan Bunch
* Date: 2/9/2018
* Version: 1.0.7
* 
* Details:
* This softwrare takes a text file that is filled with data for a raytrace render and
* sends that data to multiple servers. The servers process this data, render pixels
* and then returns them to this software. This software then takes those pixels and
* compiles them into a ppm file for viewing.
* 
* License:
* GNU GPL 3 Pulbic, Open Source
* 
* Note: Created as an educational project
*/

/*
 * Chnagelog:
 * 
 * 2/9/2018 - Version 1.0:  Developed  version 1. Developed base code.
 * 2/14/2018 - Version 1.0.1: Added the 'Color' class and the color matrix. Working on the
 *                            splitting of work between servers to render the scene data
 * 2/16/2018 - Version 1.0.2: Made allocate work function
 * 2/16/2018 - Version 1.0.3: Fine tuning of code   
 * 2/18/2018 - Version 1.0.4: Fixing the job allocation function
 * 2/19/2018 - Version 1.0.5: First working version - diabling debug mode.
 * 2/21/2018 - Version 1.0.6: Bug Fixes
 * 2/21/2018 - Version 1.0.6: Fixed output of percent
 * 2/21/2018 - Version 1.0.7: Output fixes. Optimization.
 * 2/23/2018 - Version 1.0.8: Optomization of the program by adding client-side rendering
 */



namespace RayTracer_Client
{

    public class Color {
        public double R;
        public double G;
        public double B;
        public Color(double r, double g, double b) { R = r; G = g; B = b; }
        public Color(string str)
        {
            string[] nums = str.Split(',');
            if (nums.Length != 3) throw new ArgumentException();
            R = double.Parse(nums[0]);
            G = double.Parse(nums[1]);
            B = double.Parse(nums[2]);
        }
        public static Color Make(double r, double g, double b) { return new Color(r, g, b); }
        public static Color Times(double n, Color v){ return new Color(n * v.R, n * v.G, n * v.B);}
        public static Color Times(Color v1, Color v2){return new Color(v1.R * v2.R, v1.G * v2.G, v1.B * v2.B);}
        public static Color Plus(Color v1, Color v2){return new Color(v1.R + v2.R, v1.G + v2.G, v1.B + v2.B);}
        public static Color Minus(Color v1, Color v2){return new Color(v1.R - v2.R, v1.G - v2.G, v1.B - v2.B);}
        public static readonly Color Background = Make(0, 0, 0);
        public static readonly Color DefaultColor = Make(0, 0, 0);
        public double Legalize(double d){return d > 1 ? 1 : d;}
    }

    class MainClass
    {
        //listening port number
        static int port = 60097;
        //the sender udp
        static UdpClient sender = new UdpClient(); 
        //listener udp
        static UdpClient receiver = new UdpClient(port);
        //this allows the reciever to send data to be worked on
        //static ConcurrentQueue<string> channel = new ConcurrentQueue<string>();
        static BlockingCollection<string> channel = new BlockingCollection<string>();
        //this allows the reciever to work on commands
        //static ConcurrentQueue<string> dataChannel = new ConcurrentQueue<string>();
        static BlockingCollection<String> dataChannel = new BlockingCollection<string>();
        //make thread for the reciever
        static Thread receiveProcessor = new Thread(() => receive());
        //the add data thread
        static Thread dataProcessor = new Thread(() => pixelManager());
        //make the small render thread
        static Thread smallRender = new Thread(() => smallPixelRender());
        //This keeps track of the status of work
        static bool[] allocatedWork; 

        static int winXY = 1200;
        static Color[,] bitmap = new Color[winXY, winXY];
        static int pixelCount = winXY * winXY;
        static int writeCount = pixelCount - winXY;
        static int duplicateWork = 0;

        //When we wish to send either a command or a line of the scene data, we use the method below
        public static void send(string data, string host){
            //Console.WriteLine("sending data: [{0}] to [{1}]", data, host);
            Byte[] sendBytes = Encoding.UTF8.GetBytes(data);
            sender.Send(sendBytes, sendBytes.Length, host, port);
        }

        //This is for writing the individual color values to the PPM file
        public static int ToByte(double x)
        {
            return Math.Min(255, (int)(x * 255));
        }

        //this is for writing the PPM file
        public static void WritePPM(String fileName, Color[,] bitmap, int widthheight)
        {
            var header = $"P3 {widthheight} {widthheight} 255\n";
            int co = 0;
            using (var o = new StreamWriter(fileName))
            {
                o.WriteLine(header);
                for (int y = 0; y < widthheight; y++)
                {
                    for (int x = 0; x < widthheight; x++)
                    {
                        Color c = bitmap[x, y] == null ? Color.Make(0,0,0) : bitmap[x,y];
                        if (bitmap[x, y] == null) co++;
                        o.Write($"{ToByte(c.R)} {ToByte(c.G)} {ToByte(c.B)}  ");
                    }
                    o.WriteLine();
                }
                Console.WriteLine($"Total null: {co}");
            }
        }

        public static void smallPixelRender(){
            
            while(true){
                checkMatrix(bitmap);
                int[] jobOffer = getOpenJob(0.05, true);
                var width = 1200;
                var xylow = jobOffer[0];
                var l = jobOffer[0];
                // var height = 1200;
                for (l = jobOffer[0]; l <= jobOffer[1]; l++)
                {

                    int x = l % width;
                    int y = (int)Math.Floor((double)l / width);
                    double[] cd = RayTracer.RayTracerApp.RenderPixel(x, y);
                    Color c = Color.Make(cd[0], cd[1], cd[2]);
                    //sentCount++; // RY
                                 // RY: Console.WriteLine("REndering the pixel X: {0} y: {1} c : ", x.ToString() , y.ToString(), c.ToString());
                    string r = c.R.ToString();
                    string g = c.G.ToString();
                    string b = c.B.ToString();
                    if (bitmap[x, y] == null)
                    {
                        //bitmap[x, y] = Color.Make(Convert.ToDouble(received[4]), Convert.ToDouble(received[5]), Convert.ToDouble(received[6]));
                        bitmap[x, y] = c;
                    }
                    //renderedData += ":" + r + ":" + g + ":" + b;
                    //renderedData += $":{Math.Truncate(c.R*1000}:{Math.Truncate(c.G*1000)}:{Math.Truncate(c.G*1000)}";
                }
            }
        }

        public static void pixelManager(){

            string recievedData;
            string[] received;
            bool smallRunning = false;

            while (true)
            {
                recievedData = dataChannel.Take();
                if (recievedData.Length > 0)
                {
                    received = recievedData.Split(':');
                    recievedData = "";
                    //if render data recieved, add this data to pixel matrix
                    if (received[1].Equals("renderdata"))
                    {
                        //Console.WriteLine("Renderdata recieved...");
                        //int x = Convert.ToInt32(received[2]);
                        //int y = Convert.ToInt32(received[3]);

                        //int xmin = Convert.ToInt32(recievedData[2]) % 1200;
                        //int ymin = (int)Math.Floor((Convert.ToInt32(recievedData[2])*1.0) / (1200*1.0));
                        //int xmax = Convert.ToInt32(recievedData[3]) % 1200;
                        //int ymax = (int)Math.Floor((Convert.ToInt32(recievedData[3]) * 1.0) / (1200 * 1.0));
                        int xymin = Convert.ToInt32(received[2]);
                        int xymax = Convert.ToInt32(received[3]);
                        //List<Color> cls = new List<Color>();
                        Queue<Color> cls = new Queue<Color>();
                        Color t;

                        for (int c = 4; c < received.Length; c+=3){
                            t = Color.Make(Convert.ToDouble(received[c]), Convert.ToDouble(received[c + 1]), Convert.ToDouble(received[c + 2]));
                            //Console.WriteLine(t);
                            cls.Enqueue(t);
                        }

                        //Console.WriteLine($"{jobOffer[1] - jobOffer[0]}, {cls.Count}");

                        for (int dxy = xymin; dxy < xymax-1; dxy++)// jobOffer[1]-1
                        {
                            int x = dxy % 1200;
                            int y = (int)Math.Floor((dxy * 1.0) / (1200 * 1.0));
                            //var ccls = cls.ToArray();

                           // try
                           // {//Console.WriteLine("Pixel recieved at [{0},{1}], entering into matrix", x, y);
                                if (bitmap[x, y] == null)
                                {
                                //bitmap[x, y] = Color.Make(Convert.ToDouble(received[4]), Convert.ToDouble(received[5]), Convert.ToDouble(received[6]));
                                bitmap[x, y] = cls.Dequeue(); //maybe causes error
                                    //cls.RemoveAt(0);
                                    pixelCount--;
                                    if (pixelCount % (winXY * 15) == 0)
                                    {
                                        double pp = 100 - 100 * (pixelCount * 1.0 / (winXY * winXY) * 1.0);
                                        double r = bitmap[x, y].R;
                                        double g = bitmap[x, y].G;
                                        double b = bitmap[x, y].B;
                                        //Console.Write(pixelCount + "::" + pp.ToString() + " ");
                                        Console.WriteLine("{0:0.000}% {1}px [{2:0.00}R {3:0.00}G {4:0.00}B] D:{5}", pp, pixelCount, r, g, b, duplicateWork);
                                    //Console.Flush();
                                    if (pp > 20 && smallRunning == false) { smallRender.Start(); smallRunning = true; }
                                    }
                                }
                                else
                                {
                                    duplicateWork++;
                                }
                           // }
                            /*catch (Exception e)
                            {
                                Console.WriteLine($"Error on coords: {x}, {y}");
                                throw e;
                            }*/
                        }
                        //Console.WriteLine($"[{x},{y}]:[{bitmap[x,y].ToString()}]");
                    }
                }

            }
        }

        //this is the reciever for recieveing requests from the server(s)
        public static void receive(){
            Console.WriteLine("Starting reciever...");
            while (true)
            {
                IPEndPoint pairingUtility = new IPEndPoint(IPAddress.Any, port);
                Byte[] recievedBytes = receiver.Receive(ref pairingUtility);
                string recievedData = Encoding.UTF8.GetString(recievedBytes);
                string[] data = recievedData.Split(':');
                if (data[0] == "renderdata") {
                    dataChannel.Add(pairingUtility.Address + ":" + recievedData); 
                }
                else { 
                    //channel.Enqueue(pairingUtility.Address + ":" + recievedData); 
                    channel.Add(pairingUtility.Address + ":" + recievedData); 
                }
            }
        }

        //check the matrix that contains the pixels recieved
        public static void checkMatrix(Color[,] data){
            //Console.WriteLine(" Checking the pixel matrix...");
            //Console.Write(".");
            for (int i = 0; i < data.GetLength(0); i++){
                for (int j = 0; j < data.GetLength(1); j++){
                    if (data[i, j] == null){
                        int n = data.GetLength(0)*j+i;
                        allocatedWork[n] = false;
                    }
                }
            }
        }

        //generate a matrix to handle open jobs
        public static void allocateWork(int X, int Y){
            int totalPixels = X * Y;
            allocatedWork = new bool[totalPixels];
        }

        public static int[] getOpenJob(double percent, bool isLittle){
            int buffer = Math.Max(100, Convert.ToInt32(pixelCount * percent));
            if (isLittle){ buffer = Math.Max(50, Convert.ToInt32(pixelCount * percent));}
            int[] returnValues = new int[] {-1,-1};
            int j = 0;
            for (int i = 0; i < allocatedWork.Length; i++){
                if(!allocatedWork[i]){
                    for (j = i; j < allocatedWork.Length &&  !allocatedWork[j] && j < i + buffer; j++) allocatedWork[j] = true;
                    returnValues[0] = i;
                    returnValues[1] = j-1;
                    return returnValues;
                }
            }
            return null;
        }

        public static void Main(string[] args)
        {
            string recievedData;
            string[] sceneData = File.ReadAllLines(args[0]);
            RayTracer.RayTracerApp.LoadScene(args[0]);
            string[] servers = File.ReadAllLines(args[1]);
            string[] received;
            
            if (Directory.Exists("render")){
                Console.WriteLine("Render folder exists!");
            } else {
                Directory.CreateDirectory("render");
            }
            
            receiveProcessor.Start();
            dataProcessor.Start();

            //start sending the data
            for (int i = 0; i < sceneData.Length - 1; i++)
            {
                foreach (string server in servers)
                {
                    Console.WriteLine("Sending line data....[{0}] to server [{1}]", i, server);
                    send("scene:" + sceneData.Length.ToString() + ":" + i.ToString() + ":" + sceneData[i], server);
                }
            }

            allocateWork(winXY, winXY);

            Console.WriteLine("Data sent. Waiting for response...");
            while(true){
                /*while(channel.IsEmpty){
                    //do nothing
                }*/

                //if some data is recieved, process the request
                recievedData = channel.Take();
                if (recievedData.Length > 0){
                    received = recievedData.Split(':');
                    recievedData = "";
                    //start work on the server if all scene data is recieved
                    //received[1].Equals("recieved-scene")
                    if (received[1].Equals("job-request")){
                    //Console.WriteLine("Data recieved by: [{0}]", received[0]);
                    //Console.WriteLine("Starting work on: [{0}]", received[0]);
                    job:
                        int[] jobOffer = getOpenJob(0.05, false);
                        //if (jobOffer == null) { checkMatrix(bitmap); } else { send($"startwork:{jobOffer[0]}:{jobOffer[1]}", received[0]);}
                        if (jobOffer == null) { checkMatrix(bitmap); goto job; } else { send($"startwork:{jobOffer[0]}:{jobOffer[1]}", received[0]); }
                    }

                    //if request for more data, send more/missing data
                    if (received[1].Equals("datarequest")){
                        if (received[2].Equals("all"))
                        {
                            Console.WriteLine("Server was deaf, re-sending data to server: [{0}]", received[0]);
                            for (int i = 0; i < sceneData.Length - 1; i++){
                                send(sceneData.Length + ":" + i.ToString() + ":" + sceneData[i], received[0]);
                            }
                        }
                        else
                        {
                            for (int i = 2; i < received.Length - 1; i++)
                            {
                                Console.WriteLine("Server was absent-minded, resending missing lines to: [{0}]", received[0]);
                                int lineNum = Convert.ToInt32(received[i]);
                                send(sceneData.Length.ToString() + ":" + lineNum.ToString() + ":" + sceneData[lineNum], received[0]);
                            }
                        }
                    }

                    if (pixelCount < writeCount){
                        writeCount = pixelCount - winXY*10;
                        string dt = Regex.Replace(DateTime.Now.ToString(), "[^a-zA-Z0-9]+", "-");
                        WritePPM("./render/renderData-" + dt, bitmap, winXY);
                    }
                    //Console.Write("Is matrix complete?");
                    if (pixelCount == 0){
                    //if (checkMatrix(bitmap)){
                        //Console.WriteLine(" Yes.");
                        string dt = Regex.Replace(DateTime.Now.ToString(), "[^a-zA-Z0-9]+", "-");
                        WritePPM("./render/renderData-" + dt + ".ppm", bitmap, winXY);
                        string[] serverList = File.ReadAllLines(args[1]);
                        Console.WriteLine("Data compilation is complete....telling servers...");
                        foreach (string server in serverList)
                        {
                            send("done", server);
                        }
                        Console.WriteLine("Done.");
                        break;
                    }else{
                        //Console.WriteLine(" No, continue receiving...");
                    //}
                    }
                }
            }
            Console.WriteLine("I have finished, my master.");
        }
    }
}
