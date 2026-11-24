using Strawhenge.Common.Unity.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using ILogger = Strawhenge.Common.Logging.ILogger;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity
{
    static class ProfileSources
    {
        // The profiles of the sources that are set. Settings return these as they are; the settings caches
        // Filter out the ones that can't be used.
        public static T[] Resolve<T, TSerialized, TScriptableObject>(
            SerializedSource<T, TSerialized, TScriptableObject>[] sources)
            where T : class
            where TSerialized : T, new()
            where TScriptableObject : ScriptableObject, T
        {
            if (sources == null)
                return Array.Empty<T>();

            var profiles = new List<T>(sources.Length);

            for (var i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null && sources[i].TryGetValue(out var profile))
                    profiles.Add(profile);
            }

            return profiles.ToArray();
        }

        // Keeps the profiles that can be used, in order. Null entries are skipped. Profiles findProblem
        // returns a problem for (e.g. no pivot / landing / jump asset) are skipped with a warning (once per
        // rebuild, as the settings caches keep the result), so they never match.
        public static T[] Filter<T>(
            IReadOnlyList<T> profiles,
            Func<T, string> findProblem,
            string listName,
            ILogger logger)
            where T : class
        {
            if (profiles == null)
                return Array.Empty<T>();

            var usable = new List<T>(profiles.Count);

            for (var i = 0; i < profiles.Count; i++)
            {
                var profile = profiles[i];
                if (profile == null)
                    continue;

                var problem = findProblem(profile);
                if (problem != null)
                {
                    var name = profile is Object asset ? $"'{asset.name}'" : $"{listName}[{i}]";
                    logger.LogWarning($"Profile {name} {problem} - ignoring it.");
                    continue;
                }

                usable.Add(profile);
            }

            return usable.ToArray();
        }
    }
}
