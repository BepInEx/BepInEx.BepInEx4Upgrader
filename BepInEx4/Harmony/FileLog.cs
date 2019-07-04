using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Harmony
{
    public static class FileLog
    {
        public static string logPath;

        public static char indentChar = '\t';

        public static int indentLevel;

        private static readonly List<string> buffer = new List<string>();

        static FileLog()
        {
            logPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + Path.DirectorySeparatorChar +
                      "harmony.log.txt";
            if (HarmonyInstance.DEBUG)
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                Log(string.Concat("### Harmony ", version, " started at ",
                    DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss")));
                Log("### ");
            }
        }

        private static string IndentString()
        {
            return new string(indentChar, indentLevel);
        }

        public static void ChangeIndent(int delta)
        {
            indentLevel = Math.Max(0, indentLevel + delta);
        }

        public static void LogBuffered(string str)
        {
            var obj = logPath;
            lock (obj)
            {
                buffer.Add(IndentString() + str);
            }
        }

        public static void FlushBuffer()
        {
            var obj = logPath;
            lock (obj)
            {
                using (var streamWriter = File.AppendText(logPath))
                {
                    foreach (var value in buffer) streamWriter.WriteLine(value);
                }

                buffer.Clear();
            }
        }

        public static void Log(string str)
        {
            var obj = logPath;
            lock (obj)
            {
                using (var streamWriter = File.AppendText(logPath))
                {
                    streamWriter.WriteLine(IndentString() + str);
                }
            }
        }

        public static void Reset()
        {
            var obj = logPath;
            lock (obj)
            {
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + Path.DirectorySeparatorChar +
                            "harmony.log.txt");
            }
        }

        public static unsafe void LogBytes(long ptr, int len)
        {
            var obj = logPath;
            lock (obj)
            {
                byte* ptr2 = (byte*) ptr;
                var text = "";
                for (var i = 1; i <= len; i++)
                {
                    if (text == "") text = "#  ";
                    text = text + ptr2->ToString("X2") + " ";
                    if (i > 1 || len == 1)
                    {
                        if (i % 8 == 0 || i == len)
                        {
                            Log(text);
                            text = "";
                        }
                        else if (i % 4 == 0)
                        {
                            text += " ";
                        }
                    }

                    ptr2++;
                }

                var destination = new byte[len];
                Marshal.Copy((IntPtr) ptr, destination, 0, len);
                var array = MD5.Create().ComputeHash(destination);
                var stringBuilder = new StringBuilder();
                for (var j = 0; j < array.Length; j++) stringBuilder.Append(array[j].ToString("X2"));
                Log("HASH: " + stringBuilder);
            }
        }
    }
}