using RimWorld;

namespace FactionColonies.Events
{
    [DefOf]
    public static class MoreEventsDefOf
    {
        public static FCEventDef empireEvents_plague_0;
        public static FCEventDef empireEvents_powerFailure_reroute_main;
        public static FCEventDef empireEvents_powerFailure_reroute_drain;
        public static FCEventDef empireEvents_goldenAge;
        public static FCEventDef empireEvents_growingPains_0;
        public static FCEventDef empireEvents_overextension_0;
        public static FCEventDef empireEvents_tribute_0;
        
        static MoreEventsDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MoreEventsDefOf));
        }
    }
}