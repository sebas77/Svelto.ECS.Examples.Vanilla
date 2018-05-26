using System;
using System.Diagnostics;

namespace Svelto.ECS.Vanilla.Example
{
    static public class Profile
    {
        static Stopwatch watch = new Stopwatch();
        public static int UglyCount;

        public static void It(int count, Action action1, Action action)
        {
#if PROFILE
            UglyCount = 0;
            action();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < count; i++)
                action1();
            watch.Stop();
            Utility.Console.Log(watch.ElapsedMilliseconds.ToString());
#else
            action1();
#endif
        }
    }
}