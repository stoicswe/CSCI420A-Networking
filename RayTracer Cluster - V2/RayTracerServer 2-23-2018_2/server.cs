using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace client
{
    class MainClass
    {

        static int port = 60097;
        static UdpClient sender = new UdpClient();
        static UdpClient receiver = new UdpClient(port);
        static ConcurrentQueue<string> channel = new ConcurrentQueue<string>();
        static Thread receiveProcessor = new Thread(() => receive());
        static string hostClient = "D09097";

        public static void send(string data, string host)
        {
            // RY: Console.WriteLine("Sending data to {0}", host);
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
                channel.Enqueue(recievedData);
            }
        }

        public static void MissingLines (Dictionary<int, string> dataDic) {

            String missingLines = "" ;
            for (int i = 0; i < dataDic.Count - 1; i++)
            {
                if (!dataDic.ContainsKey(i)) ;
                missingLines += i.ToString()+ ",";
            }
            send("datarequest" + missingLines,hostClient);

            //  Console.WriteLine("Missing data [{0}]", missingLines); //
           // Console.Write("$");
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
            int timeCount = 0;
            int xmin = 0;
            int xmax = 0;
            int xymin = 0;
            int ymin = 0;
            int ymax = 0;
            int xymax = 0;
            bool isReady = false;
            SortedDictionary<int, string> dataDic = new SortedDictionary<int, string>();


            //string home = Directory.GetCurrentDirectory();
            StreamWriter sceneWriter = new StreamWriter(sceneFile);

            IPHostEntry hostEntry; // get personal ip 

            hostEntry = Dns.GetHostEntry(hostClient);

            if (hostEntry.AddressList.Length > 0)
            {
                var ip = hostEntry.AddressList[0];
            }

            receiveProcessor.Start(); //start checking for incoming data
            //send ("job-request", hostClient);
            var sentCount = 0; // RY
            while (true)
            {
		if (sentCount > 0)
			Console.WriteLine($"Sent a total of {sentCount} pixels.");
		sentCount = 0;
                Console.WriteLine("Ready for instruction, waiting...");
                while (channel.IsEmpty)
                {
					timeCount++;
                   // Sort(dataDic);
					// RY: if (timeCount == 1000000 && isReady ){
			//isReady removed		
		    if (timeCount > 1000000) {
			timeCount = 0;
			if (isReady) {
				send("job-request", hostClient);
				//Console.WriteLine("requesting job- being usefull I guess");	
				//isReady = false;
			}
		    }
                }

                if (channel.TryDequeue(out recievedData))
                {
                    received = recievedData.Split(':'); // lines
                    Console.WriteLine();
                    Console.WriteLine("Data recieved, interpreting...");
                    if (received[0].Equals("scene"))
                    {
                        if (int.TryParse(received[1], out sceneLength))
                        {
                            Console.WriteLine("Scene length of {0} to be recieved.", sceneLength);
                        }

                        if (int.TryParse(received[2], out lineNum))
                        {
                            // scene.Add(lineNum + ":" + received[4]);
                            dataDic.Add(lineNum, received[3]);
                            count++;

                            Console.WriteLine("Line recieved...adding line: " + lineNum);

                        }
                    }
                    if (received[0].Equals("startwork"))
                    {
                        Console.WriteLine("startwork: " + received.ToString());
                        xymin = Convert.ToInt32(received[1]);
			xymax = Convert.ToInt32(received[2]);
			Console.WriteLine("xymin = {0}" , xymin);
			Console.WriteLine("xymax = {0}" , xymax);
			xmin = Convert.ToInt32(xymin%1200);
			Console.WriteLine("xymin = {0}" , xmin);
			ymin = Convert.ToInt32(Math.Floor((xymin*1.0)/(1200*1.0)));
			Console.WriteLine("ymin = {0}" , ymin);
			xmax = Convert.ToInt32(xymax%1200);
			Console.WriteLine("xmax = {0}" , xmax);                 
			ymax = Convert.ToInt32(Math.Floor((xymax*1.0)/(1200*1.0)));
			Console.WriteLine("ymax = {0}" , ymax);                                                           
						
			RayTracer.RayTracerApp.LoadScene("recieved.scene");
			Console.WriteLine("Loading Scene");
			var width = 1200;
			var xylow = xymin;
			var l = xymin;
			// var height = 1200;
			String renderedData = "";
			for (l = xymin; l <= xymax; l++)
			{
		                 
			        int x = l % width;
			        int y = (int)Math.Floor((double)l / width);
			        var c = RayTracer.RayTracerApp.RenderPixel(x,y);
			        sentCount++; // RY
			   // RY: Console.WriteLine("REndering the pixel X: {0} y: {1} c : ", x.ToString() , y.ToString(), c.ToString());
			        string r = c.R.ToString();
			        string g = c.G.ToString();
			        string b = c.B.ToString();
			        renderedData += ":"+r+":"+g+":"+b;
			        //renderedData += $":{Math.Truncate(c.R*1000}:{Math.Truncate(c.G*1000)}:{Math.Truncate(c.G*1000)}";
                            if (l%16 == 0)
                            {
                                var header = "renderdata:" + xylow.ToString() + ":" + l.ToString();
                                send(header + renderedData,hostClient);
                                renderedData = "";
                                xylow = l;
                            }
			}
			if (xylow != l)
			{
                            var header = "renderdata:" + xylow.ToString() + ":" + l.ToString();
                            send(header + renderedData,hostClient);
			}
			
			//Console.WriteLine("Rendered Data to be sent" + renderedData );
			Console.WriteLine( renderedData.Length );

			send(renderedData,hostClient);
                                
							
			isReady = true;
                    }
					
                    if (received[0].Equals("done"))
                    {
			Console.WriteLine("Done");
                    }
                }

                    if (count >= sceneLength-2)
                    {
						count = 0;
                        Console.WriteLine("All data recieved, begin writing data...");

                        foreach (string entry in dataDic.Values)
                        {
                            Console.WriteLine("Writing data [{0}]", entry);
                            sceneWriter.WriteLine(entry);
							
                        }
	    //sceneWriter.WriteLine("Loop is done");
	   // Console.WriteLine(" Loop is Done!");
			Console.WriteLine(dataDic.Values);
			sceneWriter.Close();
	            	Console.WriteLine("File written.");
			//while (waiting){
			send("job-request", hostClient);
			Console.WriteLine("requesting job- being usefull I guess"); 
					 
                    }
                
                }

            
            
          
           
        }
    }
}
