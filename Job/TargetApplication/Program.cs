using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace TargetApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            for (long i = 0; i < 10; i++, Thread.Sleep(500))
            {
                Console.WriteLine("X");
            }
        }
    }
}
