namespace CommonTestProject
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class MsLevelTimer
    {
        public static void Sleep(int milliSeconds)
        {
            Thread.Sleep(milliSeconds);
        }
    }
}
