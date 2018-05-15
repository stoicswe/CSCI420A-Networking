using System;


/*========================= About ===========================
 * Author: Nathan Bunch
 * Date: 4/13/2018
 * 
 * Description:
 * 
 * The WebChatServer software is designed to demonstrate
 * the use of the websocket protocol, by use of sending
 * messages from multiple clients along the protocol.
 * 
 * */

/*
 * Changelog:
 * 
 * 4/13/2018 - Begin development of the project, wrote base code, implemented an accumulator
 * 
 * */

namespace WebChatServer
{
    class Program
    {

        private static double version = 1.0;
        public static Accumulator globalAccumulator = new Accumulator();

        static void Main(string[] args)
        {
            string serverLogo = @" __________________________________________________.
|;;|                                           |;;||
|[]|-------------------------------------------|[]||
|;;|                                           |;;||
|;;|   █████▒██▓     ▒█████   ██▓███    ██████ |;;||
|;;| ▓██   ▒▓██▒    ▒██▒  ██▒▓██░  ██▒▒██    ▒ |;;||
|;;| ▒████ ░▒██░    ▒██░  ██▒▓██░ ██▓▒░ ▓██▄   |;;||
|;;| ░▓█▒  ░▒██░    ▒██   ██░▒██▄█▓▒ ▒  ▒   ██▒|;;||
|;;| ░▒█░   ░██████▒░ ████▓▒░▒██▒ ░  ░▒██████▒▒|;;||
|;;|  ▒ ░   ░ ▒░▓  ░░ ▒░▒░▒░ ▒▓▒░ ░  ░▒ ▒▓▒ ▒ ░|;;||
|;;|  ░     ░ ░ ▒  ░  ░ ▒ ▒░ ░▒ ░     ░ ░▒  ░ ░|;;||
|;;|  ░ ░     ░ ░   ░ ░ ░ ▒  ░░       ░  ░  ░  |;;||
|;;|       ░  ░    ░ ░                 ░       |;;||
|;;|___________________________________________|;;||                                         
|;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;||
|;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;||
|;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;||
|;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;||
|;;;;;;__________________________________ ;;;;;;;;||
|;;;;;|  ______                          |;;;:::;;||
|;;;;;| |;;;;;;|                         |;;;;;;;;||
|;;;;;| |;;;;;;|                         |;;;;;;;;||
|;;;;;| |;;;;;;|                         |;;;;;;;;||
|;;;;;| |;;;;;;|                         |;;;;;;;;||
|;;;;;| |;;;;;;|                         |;;;;;;;;||
|;;;;;| |;;;;;;|                         |;;;;;;;;||
|;;;;;| |______|                         |;;;;;;;;||
\_____|__________________________________|________||
 ~~~~~^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^~~~~~~~~~~~
";
            Console.WriteLine(serverLogo);
            Console.WriteLine("{0} Welcome to WCS Version: {1:N2}", globalAccumulator, version);
            Console.WriteLine("{0} Flops Version: {1:N2}", globalAccumulator, ServerInternal.GetVersion());
            Console.WriteLine("{0} IOHandle Version: {1:N2}", globalAccumulator, IOHandle.GetVersion());
            Console.WriteLine("{0} Booting the server...", globalAccumulator);
            ServerInternal serverHost = new ServerInternal();
            serverHost.Run();
            //while (true) { }
        }
    }
}
