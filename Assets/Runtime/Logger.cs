using System;
using System.Diagnostics;

namespace Observables
{
    public static class Logger
    {
        [Conditional("ENABLE_OBSERVABLE_LOGS")]
        public static void Log(string message)
        {
#if UNITY_2019_1_OR_NEWER
            UnityEngine.Debug.Log(message);
#else
            Console.WriteLine(message);
#endif
        }
    }
}
