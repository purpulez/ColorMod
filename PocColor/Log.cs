using System;
using System.IO;
using System.Reflection;


namespace PocColor
{
    internal class Log
    {
        private static StreamWriter Writer;

        static Log()
        {
            try
            {
                Log.Writer = new StreamWriter((Stream)new FileStream(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location)), "../../log.txt"), FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void write(string messageString)
        {
            try
            {
                if (Log.Writer != null)
                {
                    Log.Writer.WriteLine(messageString);
                    Log.Writer.Flush();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
