using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotcode.tests.CSTestFiles
{
    public class ClassAndConsole
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello world!");
            System.Threading.Thread.Sleep(1000 * 5);
        }

        public string GetString()
        {
            return "Test";
        }

        public void WriteToConsole()
        {
            Console.Write("This writes to the console.");
        }

        public async void AsyncMethod()
        {
            await Task.Factory.StartNew(() => { });
        }
    }
}
