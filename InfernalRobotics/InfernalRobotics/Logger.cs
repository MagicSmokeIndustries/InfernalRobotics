using System;

namespace InfernalRobotics
{
    public static class Logger
    {
        public enum Level
        {
            Fatal,
            Warning,
            Info,
            Verbose,

            // Only for debug
            Debug,
            SuperVerbose
        }

        public static void Log(string message, Level level = Level.Info)
        {
            message = "IR: " + message;
            switch (level)
            {
                case Level.Fatal:
                    UnityEngine.Debug.LogError(message);
                    break;

                case Level.Warning:
                    UnityEngine.Debug.LogWarning(message);
                    break;

                case Level.Info:
                    UnityEngine.Debug.Log(message);
                    break;

                case Level.Verbose:
                    if (GameSettings.VERBOSE_DEBUG_LOG)
                    {
                        UnityEngine.Debug.Log(message);
                    }
                    break;

                case Level.Debug:
#if DEBUG
                    UnityEngine.Debug.Log(message);
#endif
                    break;

                case Level.SuperVerbose:
#if DEBUG
                    if (GameSettings.VERBOSE_DEBUG_LOG)
                    {
                        UnityEngine.Debug.Log(message);
                    }
#endif
                    break;

                default:
                    throw new ArgumentOutOfRangeException("level");
            }
        }
    }
}