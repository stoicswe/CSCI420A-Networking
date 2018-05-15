using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;


/*============================== About ==============================
 * Author: Nathan Bunch
 * Date: 3/11/2018
 * Version: 1.11
 * 
 * Description: The circle class handles the connection of webclients
 * that are connecting via a webbrowser. This class is the heart of
 * the web server program (and therefore handles not only connections,
 * but also the sending of data from the server directory to client(s)).
 * 
 * License: GNU GPL v3
 * 
 * Note: Made for a personal project / educational submission
 * */

/*
 * 
 * Changelog:
 * 
 * 3/11/2018 - version 1.0: wrote the base code. Circle constructor, getIpAddress, loadConfig, sendHeader, connectionHandler, getLocalPath
 * 3/11/2018 - version 1.1: bug fixes
 * 3/11/2018 - version 1.11: bug fixes
 * 
 * */

namespace CircleWebHost
{
    class Circle
    {
        //common variables that are needed thoughout the software
        private static double version = 1.11;
        private int port;
        private TcpListener server;
        private IPAddress localIP = IPAddress.Parse(getLocalIPAddress());
        private string serverDirectory;
        private string webIndex;
        private bool verbotose;
        private double htmlVer;
        private double httpVer;
        
        public Circle()
        {
            //lets try stuff...if not, we dont knoe de wei....
            try
            {
                //load the configuration file and proceed (assuming the port is not null)
                loadConfig();
                server = new TcpListener(localIP, port);
                server.Start();
                //once port is open, start listening
                Thread th = new Thread(new ThreadStart(connectionHandler));
                th.Start();
                Console.WriteLine("Online.");
            } catch (Exception e)
            {
                //oops. I screwed up.
                //i did not knoe de wei.... :-(
                Console.WriteLine($"Server not started due to exception [{e.ToString()}]");
            }
        }

        public static string getLocalIPAddress()
        {
            //search for the host IP address, so that the open port is bound to this machine only.
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    //wow! I found my own name!
                    return ip.ToString();
                }
            }
            //and of course...I suck. I don't even know my own name.
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private void loadConfig()
        {
            //read the lines of the config file
            string[] cf = File.ReadAllLines("config/default.circle");
            //cfd is a temp array for handling individual lines in the config
            string[] cfd;
            //parse with me, baby!
            foreach (string line in cf)
            {
                if(line.Length > 0)
                {
                    if (line[0] == '#') { }
                    else
                    {
                        //if a match is found, set the according var to what is read from config
                        //trusting that the users are not idiots and putting constrewed values in
                        if (line.Contains("port"))
                        {
                            cfd = line.Split(" ");
                            port = int.Parse(cfd[2]);
                        }

                        if (line.Contains("webindex"))
                        {
                            int p = line.IndexOf('=');
                            webIndex = line.Substring(p+2, line.Length-(p+2));
                        }

                        if (line.Contains("local"))
                        {
                            int p = line.IndexOf('=');
                            serverDirectory = line.Substring(p + 2, line.Length - (p + 2));
                        }

                        if (line.Contains("verb"))
                        {
                            cfd = line.Split(" ");
                            if (cfd[2] == "FULL")
                            {
                                verbotose = true;
                            }

                            if (cfd[2] == "WARN")
                            {
                                verbotose = false;
                            }
                        }

                        if (line.Contains("htmlver"))
                        {
                            cfd = line.Split(" ");
                            htmlVer = Double.Parse(cfd[2]);
                        }

                        if (line.Contains("httpver"))
                        {
                            cfd = line.Split(" ");
                            httpVer = Double.Parse(cfd[2]);
                        }
                    }
                }
            }
            //hopefully there aren't any null vars left over....lol
        }

        private void connectionHandler()
        {
            //when we wanna connect with the webbrowser on a spiritual level.
            //infinite loop for the win!
            String laddr;
            while (true)
            {
                Socket connection = server.AcceptSocket();
                if (connection.Connected)
                {
                    Console.WriteLine($"<-> Client: {connection.RemoteEndPoint} | {connection.SocketType}");
                    //store dem bits
                    byte[] receive = new byte[1024];
                    //i honestly have no idea what sz is for.....I know it will do something one day!
                    int sz = connection.Receive(receive, receive.Length, 0);
                    string daBuffer = Encoding.ASCII.GetString(receive);
                    //check daBuffer for buffer things.....
                    if (daBuffer.Substring(0,3) == "GET")
                    {
                        int sp = daBuffer.IndexOf("HTTP", 1);
                        Console.WriteLine($"<-? {connection.RemoteEndPoint} | {daBuffer.Substring(0,sp)}");
                        string shV = daBuffer.Substring(sp, 8);
                        string req = daBuffer.Substring(0, sp - 1);
                        req.Replace("\\", "/");
                        if (req.IndexOf('.') < 1 && !req.EndsWith('/')) { req += "/"; }
                        //sp = req.LastIndexOf('/') + 1;
                        req = req.Split(" ")[1];
                        //Console.WriteLine(req);
                        if (req == "/") { laddr = serverDirectory + webIndex; } else { laddr = getLocalPath(req); }
                        //Console.WriteLine(laddr);
                        Console.WriteLine($"<!> Client: {connection.RemoteEndPoint} | {laddr}");
                        if (req.Length == 0)
                        {
                            string error = "<H2>Error!! Requested Directory does not exists</H2><Br>";
                            sendHeader(httpVer.ToString(), "text/html; charset=utf-8", error.Length," 404 Not Found", ref connection);
                            sendData(error, ref connection);
                            connection.Close();
                            continue;
                        }

                        if (File.Exists(laddr) == false)
                        {
                            string error = "<H2>Error!! Requested Directory does not exists</H2><Br>";
                            sendHeader(httpVer.ToString(), "text/html; charset=utf-8", error.Length, " 404 Not Found", ref connection);
                            sendData(error, ref connection);
                        }
                        else
                        {
                            //int i = 0;
                            FileStream frs = new FileStream(laddr, FileMode.Open, FileAccess.Read);
                            BinaryReader brs = new BinaryReader(frs);
                            byte[] data = new byte[frs.Length];
                            int r;
                            string srs = "";
                            while((r = brs.Read(data, 0, data.Length)) != 0){ srs += Encoding.ASCII.GetString(data, 0, r); }
                            brs.Close();
                            frs.Close();
                            sendHeader(httpVer.ToString(), "text/html; charset=utf-8", srs.Length, "200 OK", ref connection);
                            sendData(data, ref connection);
                        }
                    }
                    connection.Close();
                }
            }
        }

        private string getLocalPath(string reqd)
        {
            //do more here to simulate virtual address for files
            string fullPath;
            if (reqd == "/") { fullPath = $"{server}{webIndex}"; } else fullPath = $"{serverDirectory}{reqd}";
            return fullPath.ToLower();
        }

        /*
         * Example Header:
         * 
         *  GET / HTTP/1.1
         *  HOST: D09104
         *
         *  HTTP/1.0 200 OK
         *  Server: SimpleHTTP/0.6 Python/3.6.3
         *  Date: Sun, 11 Mar 2018 20:37:21 GMT
         *  Content-type: text/html; charset=utf-8
         *  Content-Length: 555 
         * */

        private void sendHeader(string sHttpVersion, string contentTypeHead, int iTotBytes, string sStatusCode, ref Socket connected)
        {
            //compile the header for sending to web-browser
            String sBuffer = "";
            sBuffer += $"HTTP/{sHttpVersion} {sStatusCode}\r\n";
            sBuffer += $"Server: CircleWebHost/1.0 cx1193719-b\r\n";
            sBuffer += $"Date: {DateTime.Now.ToString()}";
            sBuffer += $"Content-Type: {contentTypeHead}\r\n";
            sBuffer += $"Content-Length: {iTotBytes}\r\n\r\n";
            //converting...............
            Byte[] send = Encoding.ASCII.GetBytes(sBuffer);
            //send dem bits to the web-browser, hopefully we speak the same language
            sendData(send, ref connected);
            Console.WriteLine($"]-> Bytes: {iTotBytes.ToString()}");
        }

        private void sendData(String sData, ref Socket client)
        {
            //cheap-butt function....just calls another function lol
            sendData(Encoding.ASCII.GetBytes(sData), ref client);
        }

        private void sendData(Byte[] b, ref Socket client)
        {
            int nb = 0;
            try
            {
                //a ? b : c ~cuz iForgot
                if (client.Connected) if ((nb = client.Send(b, b.Length, 0)) == -1) Console.WriteLine("Socket error, packet not sent..."); else Console.WriteLine($"]->Bytes:{nb}");
                else Console.WriteLine("Connection Dropped....");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error sending data : {e.ToString()}");
            }
        }

        public static string getVersion()
        {
            return version.ToString();
        }
    }
}
