namespace FactionColonies.Events;

[DefOf]
public static class MoreEventsDefOf
{
    public static FCEventDef empireEvents_goldenAge;
    public static FCEventDef empireEvents_growingPains_0;
    public static FCEventDef empireEvents_tribute_0;
    public static FCEventDef empireEvents_immigrants_production_boost;
    
    static MoreEventsDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(MoreEventsDefOf));
    }
}