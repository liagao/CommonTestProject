/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SringConcatVsFormat
{
    using System.Diagnostics;
    using System.Runtime.Remoting.Messaging;
    using System.Threading;

    public class Program
    {
        private static void Main(string[] args)
        {
            Compare("hello", "world");
            Compare(new Random().Next(), new Random().Next());

            if (Debugger.IsAttached)
            {
                Console.ReadKey();
            }
        }

        private static void Compare(string a, string b)
        {

            for (int i = 0; i < 3; i++)
            {
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                var stopwatch = Stopwatch.StartNew();
                string result = null;
                stopwatch.Start();
                for (int i1 = 0; i1 < 10000000; i1++)
                {
                    result = a + ", " + b + "!";
                }
                stopwatch.Stop();

                Console.WriteLine("Run {0}: {1} took {2}. ", i, "addfun", stopwatch.ElapsedTicks);
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);

                stopwatch.Reset();
                stopwatch.Start();
                for (int i1 = 0; i1 < 10000000; i1++)
                {
                    result = string.Format("{0},{1}!", a, b);
                }

                stopwatch.Stop();
                Console.WriteLine("Run {0}: {1} took {2}. ", i, "format", stopwatch.ElapsedTicks);
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);

                stopwatch.Reset();
                stopwatch.Start();
                for (int i1 = 0; i1 < 10000000; i1++)
                {
                    result = string.Concat("{0},{1}!", a, b);
                }

                stopwatch.Stop();
                Console.WriteLine("Run {0}: {1} took {2}. ", i, "concat", stopwatch.ElapsedTicks);
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);

            }
        }

        private static void Compare<T>(T a, T b)
        {
            var flavors = new Dictionary<string, Func<T, T, int, string>>
            {
                { "format", TestFormat},
                { "addfun", TestAdd},
                { "concat", TestConcat},
            };

            for (int i = 0; i < 3; i++)
            {
                foreach (var flavor in flavors)
                {
                    
                    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                    var stopwatch = Stopwatch.StartNew();
                    string result = flavor.Value(a, b, 10000000);
                    stopwatch.Stop();

                    Console.WriteLine("Run {0}: {1} took {2}. ", i, "", stopwatch.ElapsedTicks);
                    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                }

            }
        }

        private static void Compare(int a, int b)
        {
            for (int i = 0; i < 3; i++)
            {
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                var stopwatch = Stopwatch.StartNew();
                string result = null;
                stopwatch.Start();
                for (int i1 = 0; i1 < 10000000; i1++)
                {
                    result = a + ", " + b + "!";
                }
                stopwatch.Stop();

                Console.WriteLine("Run {0}: {1} took {2}. ", i, "addfun", stopwatch.ElapsedTicks);
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);

                stopwatch.Reset();
                stopwatch.Start();
                for (int i1 = 0; i1 < 10000000; i1++)
                {
                    result = string.Format("{0},{1}!", a, b);
                }

                stopwatch.Stop();
                Console.WriteLine("Run {0}: {1} took {2}. ", i, "format", stopwatch.ElapsedTicks);
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);

                stopwatch.Reset();
                stopwatch.Start();
                for (int i1 = 0; i1 < 10000000; i1++)
                {
                    result = string.Concat("{0},{1}!", a, b);
                }

                stopwatch.Stop();
                Console.WriteLine("Run {0}: {1} took {2}. ", i, "concat", stopwatch.ElapsedTicks);
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);

            }
        }


        private static void Compare(int a, int b)
        {
            for (int i = 0; i < 3; i++)
            {

                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                //var stopwatch = Stopwatch.StartNew();
                // string result = flavor.Value(a, b, 10000000);
                var stopwatch = new Stopwatch();
                string result = null;
                stopwatch.Start();
                for (int i1 = 0; i1 < 10000000; i1++)
                {
                    result = a + ", " + b + "!";
                }
                stopwatch.Stop();

                Console.WriteLine("Run {0}: {1} took {2}. ", i, "", stopwatch.ElapsedTicks);
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);

            }
        }

        static string TestFormat<T>(T a, T b, int n)
        {
            string s = null;
            for (int i = 0; i < n; i++)
            {
                s = string.Format("{0}, {1}!", a, b);
            }

            return s;
        }

        static string TestAdd<T>(T a, T b, int n)
        {
            string s = null;
            for (int i = 0; i < n; i++)
            {
                s = a + ", " + b + "!";
            }

            return s;
        }

        static string TestConcat<T>(T a, T b, int n)
        {
            string s = null;
            for (int i = 0; i < n; i++)
            {
                s = string.Concat(a, ", ", b, "!");
            }

            return s;
        }
    }
}*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SringConcatVsFormat
{
    using System.Diagnostics;

    public class Program
    {
        private static void Main(string[] args)
        {
            Compare(1234, 5678);
            Compare("hello", "world");
            if (Debugger.IsAttached)
            {
                Console.ReadKey();
            }
        }

        private static void Compare<T>(T a, T b)
        {
            var flavors = new Dictionary<string, Func<T, T, int, string>>
            {
                { "format", TestFormat},
                { "add", TestAdd},
                { "concat", TestConcat},
            };

            for (int i = 0; i < 3; i++)
            {
                foreach (var flavor in flavors)
                {
                    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                    var stopwatch = Stopwatch.StartNew();
                    string result = flavor.Value(a, b, 10000000);
                    long duration = stopwatch.ElapsedTicks;
                    GC.Collect(2, GCCollectionMode.Forced, blocking: true);

                    Console.WriteLine("Run {0}: {1} took {2}. {3}", i, flavor.Key, duration, result);
                }
            }
        }

        static string TestFormat<T>(T a, T b, int n)
        {
            string s = null;
            for (int i = 0; i < n; i++)
            {
                s = string.Format("{0}, {1}!", a, b);
            }

            return s;
        }

        static string TestAdd<T>(T a, T b, int n)
        {
            string s = null;
            for (int i = 0; i < n; i++)
            {
                s = a + ", " + b + "!";
            }

            return s;
        }

        static string TestConcat<T>(T a, T b, int n)
        {
            string s = null;
            for (int i = 0; i < n; i++)
            {
                s = string.Concat(a, ", ", b, "!");
            }

            return s;
        }
    }
}

