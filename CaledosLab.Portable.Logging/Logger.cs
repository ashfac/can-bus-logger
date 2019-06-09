using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CaledosLab.Portable.Logging
{
    public static class Logger
    {
        private static Object _lock = new Object();
        private static int _max = 10000;

        /// <summary>
        /// max number of line logged by the system
        /// </summary>
	    public static int MaxSize
	    {
		    get { return _max;}
		    set { _max = value;}
	    }

        private static bool _enabled = true;
            
        /// <summary>
        /// enable/disable store logging
        /// </summary>
        public static bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }
        
        private static List<string> buffer { get; set; }

        public static void WriteLine(Exception e)
        {
            WriteLine ("EXCEPTION {0} {1}\n STACK TRACE {2}", e.Message, e.InnerException != null ? " HAS INNER EXCEPTION" : "", e.StackTrace);
            if (e.InnerException != null)
            {
                WriteLine(e.InnerException);
            }
        }

        public static void WriteLine(StreamWriter stream, string format, params object[] args)
        {
            string s = DateTime.Now.ToString("HH:mm:ss ") + string.Format(format, args);
            WriteLine(s);
            stream.WriteLine(s);
        }


        public static void WriteLine(StreamWriter stream, Exception e)
        {
            WriteLine(e);
            stream.WriteLine("EXCEPTION {0} {1}\n STACK TRACE {2}", e.Message, e.InnerException != null ? " HAS INNER EXCEPTION" : "", e.StackTrace);
            if (e.InnerException != null)
            {
                stream.WriteLine(e.InnerException);
            }
        }

        public static void WriteLine(string format, params object[] args)
        {
            string s = string.Format(format, args);
            WriteLine(s);
        }

        public static void WriteLine(string line)
        {
            if (Enabled)
            {
                StringBuilder sb = new StringBuilder();
                //sb.Append(DateTime.Now.ToString("yyyy.MM.dd-HH:mm:ss "));
                //sb.Append("TID");
                //sb.Append(Environment.CurrentManagedThreadId);
                //sb.Append(" ");
                sb.Append(line);

                lock (_lock)
                {                 
                    if (buffer == null)
                    {
                        buffer = new System.Collections.Generic.List<string>();
                    }

                    buffer.Add(sb.ToString());

                    while (buffer.Count() > MaxSize)
                    {
                        buffer.RemoveAt(0);
                    }
                }

                System.Diagnostics.Debug.WriteLine(sb);
            }
        }

        public static void Load(StreamReader stream)
        {
            lock (_lock)
            {
                buffer = new List<string>();

                while (!stream.EndOfStream)
                {
                    buffer.Add(stream.ReadLine());
                }
            }
        }

        public static void Save(StreamWriter stream)
        {
            if (buffer != null)
            {
                lock(_lock)
                {
                    foreach (string s in buffer)
                    {
                        stream.WriteLine(s);
                    }
                }
            }
        }

        public static string GetStoredLog()
        {
            StringBuilder sb = new StringBuilder();

            lock (_lock)
            {
                if (buffer != null)
                {
                    foreach (string s in buffer)
                    {
                        sb.AppendLine(s);
                    }
                }
            }
            
            return sb.ToString();
        }
    }
}
