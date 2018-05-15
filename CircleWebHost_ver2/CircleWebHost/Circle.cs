﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;


/*============================== About ==============================
 * Author: Nathan Bunch
 * Date: 3/11/2018
 * Version: 1.21
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
 * 3/12/2018 - version 1.12: bug fixes
 * 3/13/2018 - version 1.13: added the ability to determine content type, added the database file index to the configuration file
 * 3/14/2018 - version 1.14: added the ability to customize the POST command, also added POST handler, POST not yet implemented
 * 3/16/2018 - version 1.15: added cache for the FI (file index) file. This file will provide the virtual directory service
 * 3/21/2018 - version 1.16: added the issues page for the issues website, generated by the webserver
 * 3/22/2018 - version 1.17: made a better database storage system
 * 3/23/2018 - version 1.2: website redesign, built a new way issues are stored, updated the style for issues.
 * 3/24/2018 - version 1.21: website beautification, maybe will attract girls.....
 * */

/*
 * 
 * TODO:
 * 
 * make things look nicer
 * */

/*
 * NOTES:
 * 
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

namespace CircleWebHost
{
    class Circle
    {
        //common variables that are needed thoughout the software
        private static double version = 1.21;
        //duh....kinda important
        private int port;
        //last time I checked, I was writing a server......
        private TcpListener server;
        //who needs an IPv4? so old school (lol, internet built on this.....)
        private IPAddress localIP = IPAddress.Parse(getLocalIPAddress());
        //serverdirectory is the default directory for all server files
        private string serverDirectory;
        //webindex = index.html?
        private string webIndex;
        //webdirectory is my website files directory
        private string webdirectory;
        //dbIndex is where my database files are located
        private string dbIndex;
        //am I using the virtual disk service?
        private bool vds = false;
        //if I am, I need a list of virtual directories
        private string vdsf;
        //chache the virtual disk service file, faster than reading the HD
        private ConcurrentDictionary<String, String> vdscache = new ConcurrentDictionary<string, string>();
        //should I spam the console output?
        private bool verbotose;
        //idk why I need this, but whatever
        private double htmlVer;
        //this is sorta important
        private double httpVer;
        //what am I posting?
        private string[] post;
        //issues counter location
        private string issues;
        //issues counter file
        private string iscount;
        //issues count
        private int iIndex = 0;
        //issue storage
        private ConcurrentDictionary<int, string[]> issueStore = new ConcurrentDictionary<int, string[]>();

        public Circle()
        {
            //lets try stuff...if not, we dont knoe de wei....
            try
            {
                //load the configuration file and proceed (assuming the port is not null)
                loadConfig();
                //load the virtual directories
                //must be loaded after the config....
                loadFiFile();
                Console.WriteLine($"Listen @{localIP}:{port}");
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
        try{
            //search for the host IP address, so that the open port is bound to this machine only.
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        //wow! I found my own name!
                        if (ip.ToString().Substring(0,3) == "127") continue;
                        //Console.WriteLine(ip.ToString());
                        return ip.ToString();
                    }
                }
                //and of course...I suck. I don't even know my own name.
                throw new Exception("No network adapters with an IPv4 address in the system!");
            } catch (Exception e){
                Console.WriteLine($"Error. {e.ToString()}");
                Console.WriteLine("Using backup port: 0.0.0.0");
                return "0.0.0.0";
            }
        }

        private void loadConfig()
        {
            //read the lines of the config file
            string[] cf = File.ReadAllLines("config/config.circle");
            string[] cm = File.ReadAllLines("config/coms.circle");
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

                        if (line.Contains("db"))
                        {
                            int p = line.IndexOf('=');
                            dbIndex = line.Substring(p + 2, line.Length - (p + 2));
                        }

                        if (line.Contains("vds"))
                        {
                            int p = line.IndexOf('=');
                            vds = Convert.ToBoolean(line.Substring(p + 2, line.Length - (p + 2)).ToLower());
                        }

                        if (line.Contains("fif"))
                        {
                            int p = line.IndexOf('=');
                            vdsf = line.Substring(p + 2, line.Length - (p + 2));
                        }

                        if (line.Contains("wb"))
                        {
                            int p = line.IndexOf('=');
                            webdirectory = line.Substring(p + 2, line.Length - (p + 2));
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

            foreach (string line in cm)
            {
                if (line.Length > 0)
                {
                    if (line[0] == '#') { }
                    else
                    {
                        if (line.Contains("POST"))
                        {
                            int p = line.IndexOf('=');
                            post = line.Substring(p + 2, line.Length - (p + 2)).Split(":");
                        }
                    }
                }
            }
            //hopefully there aren't any null vars left over....lol
            issues = dbIndex + "issues/";
            if(!Directory.Exists(issues))
            {
                Directory.CreateDirectory(issues);
            }
            iscount = issues + "counter.circle";
            if(!File.Exists(iscount))
            {
                StreamWriter s = new StreamWriter(File.OpenWrite(iscount));
                s.WriteLine("0");
                s.Close();
            }

        }

        private void loadFiFile()
        {
            //load the virtual directories
            //trying to protect against hackers....lol (see how well this works....)
            string[] vscf = File.ReadAllLines($"{vdsf}");
            foreach (string line in vscf)
            {
                if (line == "") { }
                else
                {
                    if (line[0] == '#') { }
                    else
                    {
                        var kv = line.Split(";");
                        vdscache.TryAdd(kv[0], kv[1]);
                    }
                }
            }
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
                    string daBuffer = Encoding.ASCII.GetString(receive, 0, sz);
                    //check daBuffer for buffer things.....
                    try {
                        if (daBuffer.Substring(0, 3) == "GET")
                        {
                            int sp = daBuffer.IndexOf("HTTP", 1);
                            Console.WriteLine($"<-? {connection.RemoteEndPoint} | {daBuffer.Substring(0, sp)}");
                            string shV = daBuffer.Substring(sp, 8);
                            string req = daBuffer.Substring(0, sp - 1);
                            req.Replace("\\", "/");

                            if (!req.Contains("issues.xyz"))
                            {

                                if (req.IndexOf('.') < 1 && !req.EndsWith('/')) { req += "/"; }
                                req = req.Split(" ")[1];
                                if (req == "/") { laddr = serverDirectory + webIndex; } else { laddr = getLocalPath(req); }
                                Console.WriteLine($"<!> Client: {connection.RemoteEndPoint} | {laddr}");
                                if (req.Length == 0)
                                {
                                    string error = "<H2>Error!! Requested Directory does not exists</H2><Br>";
                                    sendHeader(httpVer.ToString(), "text/html; charset=utf-8", error.Length, " 404 Not Found", ref connection);
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
                                    while ((r = brs.Read(data, 0, data.Length)) != 0) { srs += Encoding.ASCII.GetString(data, 0, r); }
                                    brs.Close();
                                    frs.Close();
                                    sendHeader(httpVer.ToString(), $"text/{getContentType(laddr)}; charset=utf-8", srs.Length, "200 OK", ref connection);
                                    sendData(data, ref connection);
                                }
                            } else {
                                string issuesPage = @"<!DOCTYPE html><html><head><title>Welcome to Issued!</title><style>body { font-family: sans-serif }tbody tr:hover { background: Gainsboro;}table { border: 3px solid #808080; margin:auto;}td { padding: 10px; }td:nth-child(1){width: 64px; text-align: center}td:nth-child(2){width: 400px;}tr:nth-child(even) {background: #F0F0F0}tr:nth-child(odd)  {background: #FFFFFF}.right {float: right;margin:auto 0 auto auto;width: 60%;border: 3px solid #303030;padding: 5px;top: 10%;}.left {    float: left;margin:auto auto auto 0;    width: 30%;    border: 3px solid #303030;    padding: 5px;top: 10%;}textarea#styled {	width: 100%;    padding: 12px 20px;    margin: 8px 0;    display: inline-block;    border: 1px solid #ccc;    border-radius: 4px;    box-sizing: border-box;font-family: sans-serif;resize: none;}.clearfix {    overflow: auto;}input[type=text], select {    width: 100%;    padding: 12px 20px;    margin: 8px 0;    display: inline-block;    border: 1px solid #ccc;    border-radius: 4px;    box-sizing: border-box;font-family: sans-serif;}input[type=submit] {    width: 100%;    background-color: #4CAF50;    color: white;    padding: 14px 20px;    margin: 8px 0;    border: none;    border-radius: 4px;    cursor: pointer;}input[type=submit]:hover {background-color: #45a049;}</style></head><body><h2>A simple and broken C# issue tracking website.</h3><hr><br><div class='left'><div class='clearfix'><form action='/' method='post'><input type='text' name='fname'placeholder='Your name..'><br><textarea name='fissue' id='styled' cols='50' rows='10' placeholder='Your issue...'></textarea><br><input type='submit' value='Submit'></form></div></div><div class='right'><div class='clearfix'><table>";

                                var flattenList = issueStore.Values.ToList();
                                foreach (string[] issue in flattenList)
                                {
                                    if (issue.Length == 4)
                                    {
                                        issuesPage += $@"<tr><td>{issue[0]}</td><td>{issue[1]}</td><td><form action='/' method='post'><input type='hidden' name='index' value='{issue[2]}'><label><input name='fixed' id='checkBox' type='checkbox' value='suppaHacka_6785345' {issue[3]} >Resolved</label><input type='submit' value='Submit'></form></td></tr>";
                                    }
                                }

                                issuesPage += @"</div></div></body></html>";
                                sendHeader(httpVer.ToString(), $"text/html; charset=utf-8", issuesPage.Length, "200 OK", ref connection);
                                sendData(issuesPage, ref connection);
                            }
                        }

                        if (daBuffer.Substring(0, 4) == "POST")
                        {
                            Console.WriteLine("Recieved a POST request"); // <-[
                            Console.WriteLine(daBuffer);
                            int sp = daBuffer.IndexOf("HTTP", 1);
                            Console.WriteLine($"<-[ {connection.RemoteEndPoint} | {daBuffer.Substring(0, sp)}");
                            string shV = daBuffer.Substring(sp, 8);
                            string postData = daBuffer.Substring(0, sp - 1);

                            if (postData.Length == 0)
                            {
                                string error = "<H2>Error!! POST operation not valid.</H2><Br>";
                                sendHeader(httpVer.ToString(), "text/html; charset=utf-8", error.Length, " 400 Bad Request", ref connection);
                                sendData(error, ref connection);
                                connection.Close();
                                continue;
                            }

                            //daBuffer += "&fixed=false";
                            int start = daBuffer.IndexOf("fname");
                            int istart = daBuffer.IndexOf("index");
                            daBuffer = daBuffer.Replace('+', ' ');
                            string fullPOST = "";
                            string fixPOST = "";
                            try {
                                fullPOST = daBuffer.Substring(start, daBuffer.Length - start);
                            } catch (Exception e) { /*Console.WriteLine("Error. Default parse wont work, wrong POST, attempting second method..." + e.ToString());*/ fullPOST = ""; fixPOST = daBuffer.Substring(istart, daBuffer.Length - istart); }

                            string ff = serverDirectory + "issues.circle";
                            fullPOST = hexToAscii(fullPOST);
                            
                            if (fullPOST.Length > 0)
                            {
                                var rs = (from es in fullPOST.Split("&")
                                          let kv = es.Split("=")
                                          where kv.Length >= 2
                                          let k = kv[0]
                                          let v = kv.Skip(1).Aggregate((x, y) => x + y)
                                          select (k, v))
                                .ToDictionary(t => t.k, t => t.v);
                                fixPOST = "";

                                string[] txt = new string[] { decode(rs["fname"]), decode(rs["fissue"]), iIndex.ToString(), "" };
                                issueStore.TryAdd(iIndex, txt);
                                iIndex++;
                            }
                            if (fixPOST.Length > 0)
                            {
                                var rs = (from es in fixPOST.Split("&")
                                          let kv = es.Split("=")
                                          where kv.Length >= 2
                                          let k = kv[0]
                                          let v = kv.Skip(1).Aggregate((x, y) => x + y)
                                          select (k, v))
                                .ToDictionary(t => t.k, t => t.v);

                                if (rs.Count() >= 1 && rs.ContainsKey("index"))
                                {
                                    int r = int.Parse(rs["index"]);
                                    string[] issue = new string[4];
                                    issueStore.TryGetValue(r, out issue);
                                    if (issue[3] == "checked")
                                    {
                                        issue[3] = "";
                                    }
                                    else issue[3] = "checked";
                                    issueStore.TryAdd(r, issue);
                                }
                            }
                            string issuesPage = "";
                            if (DateTime.Now.Month == 4 && DateTime.Now.Day == 1)
                            {
                                issuesPage = @"<!DOCTYPE html><html><head><title>Welcome to Issued!</title><style>body { font-family: sans-serif }tbody tr:hover { transition: transform 100s; transform: rotate(180deg);s background: Gainsboro;}table { border: 3px solid #808080; margin:auto;}td { padding: 10px; }td:nth-child(1){width: 64px; text-align: center}td:nth-child(2){width: 400px;}tr:nth-child(even) {background: #F0F0F0}tr:nth-child(odd)  {background: #FFFFFF}.right {float: right;margin:auto 0 auto auto;width: 60%;border: 3px solid #303030;padding: 5px;top: 10%;}.left {    float: left;margin:auto auto auto 0;    width: 30%;    border: 3px solid #303030;    padding: 5px;top: 10%;}textarea#styled {	width: 100%;    padding: 12px 20px;    margin: 8px 0;    display: inline-block;    border: 1px solid #ccc;    border-radius: 4px;    box-sizing: border-box;font-family: sans-serif;resize: none;}.clearfix {    overflow: auto;}input[type=text], select {    width: 100%;    padding: 12px 20px;    margin: 8px 0;    display: inline-block;    border: 1px solid #ccc;    border-radius: 4px;    box-sizing: border-box;font-family: sans-serif;}input[type=submit] {    width: 100%;    background-color: #4CAF50;    color: white;    padding: 14px 20px;    margin: 8px 0;    border: none;    border-radius: 4px;    cursor: pointer;}input[type=submit]:hover {background-color: #45a049;}</style></head><body><h2>A simple and broken C# issue tracking website.</h3><hr><br><div class='left'><div class='clearfix'><form action='/' method='post'><input type='text' name='fname'placeholder='Your name..'><br><textarea name='fissue' id='styled' cols='50' rows='10' placeholder='Your issue...'></textarea><br><input type='submit' value='Submit'></form></div></div><div class='right'><div class='clearfix'><table>";
                            }
                            else
                            {
                                issuesPage = @"<!DOCTYPE html><html><head><title>Welcome to Issued!</title><style>body { font-family: sans-serif }tbody tr:hover { background: Gainsboro;}table { border: 3px solid #808080; margin:auto;}td { padding: 10px; }td:nth-child(1){width: 64px; text-align: center}td:nth-child(2){width: 400px;}tr:nth-child(even) {background: #F0F0F0}tr:nth-child(odd)  {background: #FFFFFF}.right {float: right;margin:auto 0 auto auto;width: 60%;border: 3px solid #303030;padding: 5px;top: 10%;}.left {    float: left;margin:auto auto auto 0;    width: 30%;    border: 3px solid #303030;    padding: 5px;top: 10%;}textarea#styled {	width: 100%;    padding: 12px 20px;    margin: 8px 0;    display: inline-block;    border: 1px solid #ccc;    border-radius: 4px;    box-sizing: border-box;font-family: sans-serif;resize: none;}.clearfix {    overflow: auto;}input[type=text], select {    width: 100%;    padding: 12px 20px;    margin: 8px 0;    display: inline-block;    border: 1px solid #ccc;    border-radius: 4px;    box-sizing: border-box;font-family: sans-serif;}input[type=submit] {    width: 100%;    background-color: #4CAF50;    color: white;    padding: 14px 20px;    margin: 8px 0;    border: none;    border-radius: 4px;    cursor: pointer;}input[type=submit]:hover {background-color: #45a049;}</style></head><body><h2>A simple and broken C# issue tracking website.</h3><hr><br><div class='left'><div class='clearfix'><form action='/' method='post'><input type='text' name='fname'placeholder='Your name..'><br><textarea name='fissue' id='styled' cols='50' rows='10' placeholder='Your issue...'></textarea><br><input type='submit' value='Submit'></form></div></div><div class='right'><div class='clearfix'><table>";
                            }
                            var flattenList = issueStore.Values.ToList();
                            foreach (string[] issue in flattenList)
                            {
                                if (issue.Length == 4)
                                {
                                    issuesPage += $@"<tr><td>{issue[0]}</td><td>{issue[1]}</td><td><form action='/' method='post'><input type='hidden' name='index' value='{issue[2]}'><label><input name='fixed' id='checkBox' type='checkbox' value='suppaHacka_6785345' {issue[3]} >Resolved</label><input type='submit' value='Submit'></form></td></tr>";
                                }
                            }
                            issuesPage += "</div></div></body></html>";
                            sendHeader(httpVer.ToString(), $"text/html; charset=utf-8", issuesPage.Length, "200 OK", ref connection);
                            sendData(issuesPage, ref connection);

                        }
                        connection.Close();
                    } catch (Exception e) { Console.WriteLine(e); }
                    }
            }
        }

        enum State { Normal, SawPercent, InPercent };


        private string hexToAscii(string ln) //fix this to work
        {
            IEnumerable<char> ToChars(string s) {
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
                } }
            return new string(ToChars(ln).ToArray());
        }

        private string decode(string text)
        {
            string tmp = text;
            tmp = tmp.Replace("&", "&amp;");
            tmp = tmp.Replace("<", "&lt;");
            tmp = tmp.Replace(">", "&gt;");
            return tmp;
        }

        private string getLocalPath(string reqd)
        {
            //do more here to simulate virtual address for files
            string fullPath;
            if (vds)
            {
                if (vdscache.ContainsKey(reqd)) {
                    int end = reqd.Length - 1;
                    if (reqd == "/")
                    {
                        fullPath = $"{webdirectory}{vdscache[reqd]}";
                    }
                    else
                        fullPath = $"{webdirectory}{vdscache[reqd]}";
                }
                else
                {
                    return "";
                }
            }
            else
            {
                int end = reqd.Length - 1;
                if (reqd == "/")
                {
                    fullPath = $"{webdirectory}{webIndex}";
                }
                else
                    fullPath = $"{webdirectory}{reqd.Substring(1, end)}";
            }
            return fullPath.ToLower();
        }

        private static string getContentType(string path)
        {
            int n = path.LastIndexOf('.')+1;
            int l = path.Length - n;
            string ct = path.Substring(n, l);
            //Console.WriteLine(ct);
            return ct;
        }

        private void sendHeader(string sHttpVersion, string contentTypeHead, int iTotBytes, string sStatusCode, ref Socket connected)
        {
            //compile the header for sending to web-browser
            String sBuffer = "";
            sBuffer += $"HTTP/{sHttpVersion} {sStatusCode}\r\n";
            sBuffer += $"Server: CircleWebHost/1.0 cx1193719-b\r\n";
            sBuffer += $"Date: {DateTime.Now.ToString()}\r\n";
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