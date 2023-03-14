using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;

namespace ConsoleApp1
{
    public class NodeInfo
    {
        public string NodeName { get; set; }
        public double P95CPNodeLatencyAvg { get; set; }
        public double P95CPNodePercentage { get; set; }
        public double P95CPNodeContribution { get; set; }
        public double P95NodeLatencyAvg { get; set; }
        public double P95NodePercentage { get; set; }
        public double P95NodeContribution { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain myDomain = Thread.GetDomain();
            myDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            var myPrincipal = Thread.CurrentPrincipal;
            Console.WriteLine("{0} belongs to: ", myPrincipal.IsInRole("xapbeijing"));
        }
    }
}
