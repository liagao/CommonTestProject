using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            string str1 = "TestMethod1";
            Console.WriteLine(str1.GetHashCode());
            Console.ReadLine();
        }
    }
}
