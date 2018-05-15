using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

/*============================== About ==============================
 * Author: Nathan Bunch
 * Date: 3/11/2018
 * Version: 1.00
 * 
 * Description: 
 * The IOHandle class handles all the disk read/write operations.
 * Primarily Program.globalAccumulator, this, is for handling a server configuration file.
 * 
 * Note: Made for a personal project / educational submission
 * */

/*
 * 
 * Changelog:
 * 
 * 4/13/2018 - Basic code structure written, implemented verbose, read/write
 * */

namespace WebChatServer
{
    class IOHandle
    {
        private static double version = 1.00;
        private string filePath = "";
        private string fileType = ".flop";
        private bool verbose = true;

        public void SetPath(string path)
        {
            if (verbose) { Console.WriteLine("{0} {1} Set internal path to: {2}", Program.globalAccumulator, this, path); }
            filePath = path;
        }

        public void SetPath()
        {
            if (verbose) { Console.WriteLine("{0} {1} Set internal path to: {2}", Program.globalAccumulator, this, Directory.GetCurrentDirectory()); }
            filePath = Directory.GetCurrentDirectory();
        }

        public void WriteLine(string line, string filename)
        {
            if (!File.Exists(filePath + filename + fileType)) {
                if (verbose) { Console.WriteLine("{0} {1} File does not exist...creating: {2}", Program.globalAccumulator, this, filename + fileType); }
                File.Create(filePath + filename + fileType);
            }
            if (verbose) { Console.WriteLine("{0} {1} Appending contents to file: {2}", Program.globalAccumulator, this, filename + fileType); }
            File.AppendAllText(filePath + filename + fileType, line);
        }

        public void WriteLines(string[] lines, string filename)
        {
            if (!File.Exists(filePath + filename + fileType)) {
                if (verbose) { Console.WriteLine("{0} {1} File does not exist...creating: {2}", Program.globalAccumulator, this, filename + fileType); }
                File.Create(filePath + filename + fileType);
            }
            if (verbose) { Console.WriteLine("{0} {1} Appending contents to file: {2}", Program.globalAccumulator, this, filename + fileType); }
            File.AppendAllLines(filePath + filename + fileType, lines);
        }

        public bool TryWriteLine(string line, string filename)
        {
            bool success = false;
            try
            {
                if (!File.Exists(filePath + filename + fileType)) {
                    if (verbose) { Console.WriteLine("{0} {1} File does not exist...creating: {2}", Program.globalAccumulator, this, filename + fileType); }
                    File.Create(filePath + filename + fileType);
                }
                if (verbose) { Console.WriteLine("{0} {1} Appending contents to file: {2}", Program.globalAccumulator, this, filename + fileType); }
                File.AppendAllText(filePath + filename + fileType, line);
                success = true;
            } catch
            {
                Console.WriteLine("ERROR: File not found or file busy.");
            }
            return success;
        }

        public bool TryWriteLines(string[] lines, string filename)
        {
            bool success = false;
            try
            {
                if (!File.Exists(filePath + filename + fileType)) {
                    if (verbose) { Console.WriteLine("{0} {1} File does not exist...creating: {2}", Program.globalAccumulator, this, filename + fileType); }
                    File.Create(filePath + filename + fileType);
                }
                if (verbose) { Console.WriteLine("{0} {1} Appending contents to file: {2}", Program.globalAccumulator, this, filename + fileType); }
                File.AppendAllLines(filePath + filename + fileType, lines);
                success = true;
            }
            catch (IOException e)
            {
                Console.WriteLine("ERROR: File not found or file busy.");
            }
            return success;
        }

        public string[] ReadFile(string filename) {
            if (verbose) { Console.WriteLine("{0} {1} Reading file contents: {2}", Program.globalAccumulator, this, filename + fileType); }
            string[] rl = new string[0];
            try
            {
                if (verbose) { Console.WriteLine("{0} {1} Reading file: {2}", Program.globalAccumulator, this, filename + fileType); }
                rl = File.ReadAllLines(filePath + filename + fileType);
            }
            catch (IOException e)
            {
                Console.WriteLine("ERROR: File not found while attempting to read.");
            }
            return rl;
        }

        public static double GetVersion() { return version; }
        public void SetVerbose(bool value) { verbose = value; }
    }
}
