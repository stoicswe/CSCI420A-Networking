using System;
using System.IO;
using System.Text;

/*============================== About ==============================
 * Author: Nathan Bunch
 * Date: 3/11/2018
 * Version: 1.11
 * 
 * Description: The Program class runs and initializes various circle
 * sub-classes within the namespace: CircleWebHost. Eventually the
 * webserver should be able to host more than one website at a time.
 * 
 * License: GNU GPL v3
 * 
 * Note: Made for a personal project / educational submission
 * */

/*
 * 
 * Changelog:
 * 
 * 3/11/2018 - version 1.0: generated the base code (main function)
 * 3/11/2018 - version 1.1: beep added
 * 3/11/2018 - version 1.11: removed beep
 * 
 * */

namespace CircleWebHost
{
    class Program
    {

        private static double version = 1.11;

        static void Main(string[] args)
        {
            Console.WriteLine($"Welcome to Circle WebHost Ver. {version}");
            Console.WriteLine($"Circle Master Head Ver. {Circle.getVersion()}");
            Console.WriteLine("Booting up...");
            Circle mainCircle = new Circle();
            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Escape) break;
            }
        }
    }
}
