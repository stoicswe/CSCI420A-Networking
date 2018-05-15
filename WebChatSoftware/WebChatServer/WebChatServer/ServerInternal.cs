using System.Net.Sockets;
using System.Net;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/*============================== About ==============================
 * Author: Nathan Bunch
 * Date: 3/11/2018
 * Version: 1.00
 * 
 * Description: 
 * The ServerInternal class handles all the server related
 * functionality. Connections, websockets, and the web host are all handled here.
 * 
 * Note: Made for a personal project / educational submission
 * */

/*
 * 
 * Changelog:
 * 
 * 4/13/2018 - Basic code structure written, implemented websocket (basic, need more functionality), implemented verbose option
 * 4/18/2018 - Added the ability to serve a webpage, then switch to websocket
 * 4/23/2018 - Worked on reorganizing code. Got websocket connection to work for safari, chrome is tempermental about this
 * */

namespace WebChatServer
{
    class ServerInternal
    {
        private static bool verbose = false;
        private static double version = 1.00;
        private IOHandle iOHandler = new IOHandle();
        private int listeningPort = 8080;
        private TcpListener tcpListener;
        private IPAddress localIP;
        private ConcurrentDictionary<int, Message> messageCache = new ConcurrentDictionary<int, Message>();
        private Accumulator messageCount = new Accumulator();
        private ConcurrentDictionary<int, Client> connectedClients = new ConcurrentDictionary<int, Client>();
        private BlockingCollection<Message> messages = new BlockingCollection<Message>(50);

        public ServerInternal()
        {
            if (verbose) { Console.WriteLine("{0} {1} Initializing FLOPS server internal...", Program.globalAccumulator, this); }
            iOHandler.SetPath();
            ReadConfigFile();
            localIP = IPAddress.Parse(GetLocalIPAddress());
        }

        private void ReadConfigFile()
        {
            if (verbose) { Console.WriteLine("{0} {1} Reading FLOPS config file...", Program.globalAccumulator, this); }
            string[] cf = iOHandler.ReadFile("config");
            if (cf.Length > 0)
            {
                foreach (string ln in cf)
                {
                    if (ln.Length > 0)
                    {
                        if (ln[0] != '#')
                        {
                            string[] ca = ln.Split('=');
                            if (ca.Length >= 2)
                            {
                                if (ca[0] == "verbose")
                                {
                                    try
                                    {
                                        verbose = bool.Parse(ca[1]);
                                    }
                                    catch
                                    {
                                        Console.WriteLine("Could not read the verbose option, using default: {1}", verbose);
                                    }
                                }

                                if (ca[0] == "port")
                                {
                                    try
                                    {
                                        //listeningPort = int.Parse(ca[1]);
                                    }
                                    catch
                                    {
                                        Console.WriteLine("Could not read the port number, using default: {0} {1}", listeningPort);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("ERROR: File not read. Using default values");
            }
        }

        public void Run()
        {
            try
            {
                tcpListener = new TcpListener(localIP, listeningPort);
                Console.WriteLine("{0} {1} FLOPS server listening on: {2}:{3}.", Program.globalAccumulator, this, localIP, listeningPort);
                tcpListener.Start();
                var mss = new Thread(new ThreadStart(MessageSendService));
                mss.Start();
                try
                {
                    while (true)
                    {
                        var cli = tcpListener.AcceptTcpClient();
                        var th = new Thread(new ThreadStart(() => ConnectionHandle(cli)));
                        th.Start();
                    }
                } finally
                {
                    tcpListener.Stop();
                    Console.WriteLine("I have stopped. :-/");
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("FATAL: Server not started due to exception.");
            }
        }

        private void ConnectionHandle(TcpClient connection)
        {
            NetworkStream dataLink = connection.GetStream();
            bool isWebsocketConnection = false;
            if (verbose) { Console.WriteLine("{0} {1} FLOPS server accepted connection from: {2}|{3}.", Program.globalAccumulator, this, connection.Client, connection.GetType()); }
            if (connection.Connected)
            {
                try
                {
                    if (verbose) { Console.WriteLine("{0} {1} FLOPS opening datalink with: {2}|{3}.", Program.globalAccumulator, this, connection.Client, connection.GetType()); }
                    dataLink = connection.GetStream();
                    if (verbose) { Console.WriteLine("{0} {1} FLOPS recieved a request: {2}|{3}.", Program.globalAccumulator, this, connection.Client, connection.GetType()); }
                    string[] headRequest = ReadHeader(dataLink);
                    string data = headRequest[0];
                    if (verbose) { Console.WriteLine("{0} {1} FLOPS receied data request: {2}.", Program.globalAccumulator, this, data); }

                    try
                    {
                        if (Regex.IsMatch(data, "(^GET /ws)"))
                        {
                            string key = "THIS IS A KEY";
                            foreach (var l in headRequest)
                            {
                                if (l.StartsWith("Sec-WebSocket-Key:"))
                                {
                                    key = l.Split(":", 2)[1].Trim();
                                }
                            }
                            const string el = "\r\n";
                            if (verbose) { Console.WriteLine("{0} {1} FLOPS switching connection to WebSocket...", Program.globalAccumulator, this); }
                            Byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + el
                            + "Connection: Upgrade" + el
                            + "Upgrade: websocket" + el
                            + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                            System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))) + el
                                + "Sec-WebSocket-Protocol: Chatting" + el + el); //renamed to "Chatting" || "issues"
                            dataLink.Write(response, 0, response.Length);
                            var cc = new Client(connection, dataLink, messages, verbose);
                            connectedClients.TryAdd(Program.globalAccumulator.getValue(), cc);
                            cc.Run();
                        }
                        else if ((Regex.IsMatch(data, "(^GET /index.html)") && !isWebsocketConnection))
                        {
                            if (verbose) { Console.WriteLine("{0} {1} FLOPS handling connection request, serving webpage(s) to {2}", Program.globalAccumulator, this, connection.Client); }
                            string website = Page.getPage();
                            string header = Header("html", website.Length, "200");
                            byte[] encodedHeader = Encoding.UTF8.GetBytes(header);
                            byte[] encodedWebsite = Encoding.UTF8.GetBytes(website);
                            dataLink.Write(encodedHeader, 0, header.Length);
                            dataLink.Write(encodedWebsite, 0, encodedWebsite.Length);
                            dataLink.Close();
                            connection.Close();
                        }
                        else if ((Regex.IsMatch(data, "(^GET /favicon)")))
                        {
                            if (verbose) { Console.WriteLine("{0} {1} FLOPS handling connection request, serving webpage(s) to {2}", Program.globalAccumulator, this, connection.Client); }
                            string website = "<html>NOTHING</html>";
                            string header = Header("html", website.Length, "404");
                            byte[] encodedHeader = Encoding.UTF8.GetBytes(header);
                            byte[] encodedWebsite = Encoding.UTF8.GetBytes(website);
                            dataLink.Write(encodedHeader, 0, header.Length);
                            dataLink.Write(encodedWebsite, 0, encodedWebsite.Length);
                            dataLink.Close();
                            connection.Close();
                        }
                    }
                    catch
                    {

                    }
                    finally
                    {
                        connection.Close();
                    }
                }
                catch
                {

                }
                finally
                {
                    connection.Close();
                }
            }
        }

        private void MessageSendService()
        {
            while (true)
            {
                Message sm = null;
                sm = messages.Take();
                foreach (var cli in connectedClients)
                {
                    var c = sm.GetClient;
                    if(c != cli.Value)
                    {
                        cli.Value.Send(sm);
                    }
                }
                sm = null;
            }
        }

        private string Header(string contentTypeHead, int iTotBytes, string sStatusCode)
        {
            //compile the header for sending to web-browser
            String sBuffer = "";
            sBuffer += $"HTTP/1.1 {sStatusCode}\r\n";
            sBuffer += $"Server: CircleWebHost/1.0 cx1193719-b\r\n";
            sBuffer += $"Date: {DateTime.Now.ToString()}\r\n";
            sBuffer += $"Content-Type: {contentTypeHead}\r\n";
            sBuffer += $"Content-Length: {iTotBytes}\r\n\r\n";
            //converting...............
            Byte[] send = Encoding.ASCII.GetBytes(sBuffer);
            //send dem bits to the web-browser, hopefully we speak the same language
            if (verbose) { Console.WriteLine("{0} {1} FLOPS sending header....", Program.globalAccumulator, this); };
            return sBuffer;
        }
        private string ReadLine(NetworkStream data)
        {
            StringBuilder lines = new StringBuilder();
            while (true)
            {
                var b = Convert.ToChar(data.ReadByte());
                if (b == '\n')
                    break;
                lines.Append(b.ToString());
            }
            return lines.ToString();
        }
        private string[] ReadHeader(NetworkStream data)
        {
            var lines = new List<String>();
            while (true) {
                var line = ReadLine(data);
                if (line.Trim() == "")
                    break;
                lines.Add(line);
            }
            return lines.ToArray();
        }
        public static string GetLocalIPAddress()
        {
            try
            {
                if (verbose) { Console.WriteLine("{0} Getting host IP address...", Program.globalAccumulator); }
                //search for the host IP address, so that the open port is bound to Program.globalAccumulator, this, machine only.
                var host = Dns.GetHostEntry(Dns.GetHostName());
                if (verbose) { Console.WriteLine("{0} Host DNS entry: {1}", Program.globalAccumulator, host.HostName); }
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        //wow! I found my own name!
                        if (ip.ToString().Substring(0, 3) == "127") continue;
                        //Console.WriteLine(ip.ToString());
                        if (verbose) { Console.WriteLine("{0} Host IP Found: {1}", Program.globalAccumulator, ip.ToString()); }
                        return ip.ToString();
                    }
                }
                //and of course...I suck. I don't even know my own name.
                throw new Exception("No network adapters with an IPv4 address in the system!");
            }
            catch (Exception e)
            {
                //Console.WriteLine($"Error. {e.ToString()}");
                Console.WriteLine("ERROR: Using backup port: 0.0.0.0");
                return "0.0.0.0";
            }
        }
        enum State { Normal, SawPercent, InPercent };
        private string HexToAscii(string ln)
        {
            IEnumerable<char> ToChars(string s)
            {
                State state = State.Normal;
                char first = '\0';
                foreach (var c in s)
                {
                    switch (state)
                    {
                        case State.Normal:
                            switch (c)
                            {
                                case '+':
                                    yield return ' ';
                                    break;
                                case '%':
                                    state = State.SawPercent;
                                    break;
                                default:
                                    yield return c;
                                    break;
                            }
                            break;
                        case State.SawPercent:
                            first = c;
                            state = State.InPercent;
                            break;
                        case State.InPercent:
                            if (int.TryParse($"{first}{c}", NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var i))
                                yield return Convert.ToChar(i);
                            state = State.Normal;
                            break;
                    }
                }
            }
            return new string(ToChars(ln).ToArray());
        }
        private string FixInputs(string text)
        {
            string tmp = text;
            tmp = tmp.Replace("&", "&amp;");
            tmp = tmp.Replace("<", "&lt;");
            tmp = tmp.Replace(">", "&gt;");
            return tmp;
        }
        public static double GetVersion(){ return version;}
        private void SetVerbose(bool value) { verbose = value; }
    }
}
