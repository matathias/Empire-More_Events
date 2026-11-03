using System.Collections.Generic;

namespace FactionColonies.Events
{
    /// <summary>
    /// Shared helpers for the More Events tests: a snapshot/restore for the base-mod
    /// <see cref="FCSettings"/> statics the tests pin (so a test can set a known value and still
    /// leave settings as it found them), plus small throwaway def/event builders for the pure
    /// condition-predicate and eligibility tests. The built defs are never registered in the
    /// DefDatabase — reference identity is all these tests rely on.
    /// </summary>
    public static class EETestHelper
    {
        /// <summary>Captured values of the base-mod settings statics that the tests may overwrite.</summary>
        public struct SettingsSnapshot
        {
            public bool disableEventsWithOptions;
        }

        public static SettingsSnapshot SnapshotSettings()
        {
            SettingsSnapshot s;
            s.disableEventsWithOptions = FCSettings.disableEventsWithOptions;
            return s;
        }

        public static void RestoreSettings(SettingsSnapshot s)
        {
            FCSettings.disableEventsWithOptions = s.disableEventsWithOptions;
        }

        /// <summary>A throwaway <see cref="FCEventDef"/> keyed only by defName / incompatibility list.</summary>
        public static FCEventDef MakeEventDef(string defName, List<FCEventDef> incompatibleEvents = null)
        {
            return new FCEventDef
            {
                defName = defName,
                incompatibleEvents = incompatibleEvents ?? new List<FCEventDef>()
            };
        }

        /// <summary>A throwaway <see cref="FCEvent"/> wrapping <paramref name="def"/> (default def is empty otherwise).</summary>
        public static FCEvent MakeEvent(FCEventDef def)
        {
            return new FCEvent { def = def };
        }

        /// <summary>A throwaway <see cref="FCOptionDef"/> with the given success chance.</summary>
        public static FCOptionDef MakeOptionDef(float baseChanceOfSuccess)
        {
            return new FCOptionDef { baseChanceOfSuccess = baseChanceOfSuccess };
        }
    }
}
