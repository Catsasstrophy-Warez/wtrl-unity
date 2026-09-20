using System.Collections.Generic;

namespace WTRL.Core
{
    /// <summary>
    /// The project's first analytics infrastructure -- closes "no
    /// analytics opt-in" from the Milestone M8 gap audit.
    ///
    /// HONEST SCOPE: there is no real analytics vendor SDK wired up
    /// anywhere in this project (no Firebase/Amplitude/etc. package
    /// reference exists), and adding one without a real account/API key
    /// would just be an unverifiable stub. What this actually provides
    /// is the real, necessary PRECONDITION for that integration: a
    /// consent gate that defaults to opted-OUT (never assume consent),
    /// an in-memory event log that a real backend would drain instead of
    /// this pass inventing one, and the discipline that no event is ever
    /// recorded without consent -- checked in code, not just documented.
    /// Wiring a real vendor SDK as the actual sink is separate, real
    /// future work requiring an actual account, not something fakeable
    /// here.
    /// </summary>
    public static class AnalyticsConsent
    {
        /// <summary>Defaults to false -- analytics must be explicitly
        /// opted into, never assumed.</summary>
        public static bool OptedIn { get; set; } = false;

        private static readonly List<AnalyticsEvent> Log = new();

        public static IReadOnlyList<AnalyticsEvent> RecordedEvents => Log;

        /// <summary>Records an event only if the player has opted in --
        /// silently a no-op otherwise (not an error; most of this
        /// project's runtime code shouldn't have to check
        /// <see cref="OptedIn"/> itself before calling this).</summary>
        public static void Record(string eventName, IReadOnlyDictionary<string, string> parameters = null)
        {
            if (!OptedIn) return;
            Log.Add(new AnalyticsEvent(eventName, parameters ?? EmptyParameters));
        }

        /// <summary>Test/debug hook -- clears the in-memory log without
        /// touching the consent flag.</summary>
        public static void ClearLog() => Log.Clear();

        private static readonly IReadOnlyDictionary<string, string> EmptyParameters = new Dictionary<string, string>();
    }

    public readonly struct AnalyticsEvent
    {
        public readonly string Name;
        public readonly IReadOnlyDictionary<string, string> Parameters;

        public AnalyticsEvent(string name, IReadOnlyDictionary<string, string> parameters)
        {
            Name = name;
            Parameters = parameters;
        }
    }
}
