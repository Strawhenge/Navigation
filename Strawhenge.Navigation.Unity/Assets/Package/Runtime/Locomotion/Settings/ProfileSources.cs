using Strawhenge.Common.Unity.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity
{
    static class ProfileSources
    {
        // Resolves serialized profile sources to the profiles that can be used. Unset sources are skipped.
        // See Filter for profiles that can't be used.
        public static T[] Resolve<T, TSerialized, TScriptableObject>(
            SerializedSource<T, TSerialized, TScriptableObject>[] sources,
            Func<T, string> findProblem,
            string fieldName,
            Object context)
            where T : class
            where TSerialized : T, new()
            where TScriptableObject : ScriptableObject, T
        {
            if (sources == null)
                return Array.Empty<T>();

            var profiles = new T[sources.Length];

            for (var i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null && sources[i].TryGetValue(out var profile))
                    profiles[i] = profile;
            }

            return Filter(profiles, findProblem, fieldName, context);
        }

        // Keeps the profiles that can be used, in order. Null entries are skipped. Profiles findProblem
        // returns a problem for (e.g. no pivot / landing / jump asset) are skipped with a warning (once, as
        // the settings cache the result), so they never match.
        public static T[] Filter<T>(
            IReadOnlyList<T> profiles,
            Func<T, string> findProblem,
            string fieldName,
            Object context)
            where T : class
        {
            var usable = new List<T>(profiles.Count);

            for (var i = 0; i < profiles.Count; i++)
            {
                var profile = profiles[i];
                if (profile == null)
                    continue;

                var problem = findProblem(profile);
                if (problem != null)
                {
                    var name = profile is Object asset ? $"'{asset.name}'" : $"{fieldName}[{i}]";
                    Debug.LogWarning($"Profile {name} {problem} - ignoring it.", context);
                    continue;
                }

                usable.Add(profile);
            }

            return usable.ToArray();
        }
    }
}
