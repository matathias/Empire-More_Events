using Verse;

namespace FactionColonies.Events
{
    public static class LogUtil
    {
        public const string Slug = "[Empire-Events]";

        public static void Message(string message)
        {
            if (EmpireEventsSettings.PrintDebug)
                Log.Message(Slug + " " + message);
        }

        public static void Warning(string message)
        {
            Log.Warning(Slug + " " + message);
        }

        public static void Error(string message)
        {
            Log.Error(Slug + " " + message);
        }
    }
}
