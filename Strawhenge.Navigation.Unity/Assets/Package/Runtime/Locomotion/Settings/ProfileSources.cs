using Strawhenge.Common.Unity.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity
{
    static class ProfileSources
    {
        // Resolves serialized profile sources to the profiles that can be used. Unset sources are skipped,
        // and profiles with no ID (no pivot / landing asset) are skipped with a warning (once, as the
        // settings cache the result), so they never match.
        public static T[] Resolve<T, TSerialized, TScriptableObject>(
            SerializedSource<T, TSerialized, TScriptableObject>[] sources,
            Func<T, int?> getId,
            string fieldName,
            Object context)
            where T : class
            where TSerialized : T, new()
            where TScriptableObject : ScriptableObject, T
        {
            if (sources == null)
                return Array.Empty<T>();

            var profiles = new List<T>(sources.Length);

            for (var i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null || !sources[i].TryGetValue(out var profile))
                    continue;

                if (getId(profile) == null)
                {
                    var name = profile is Object asset ? $"'{asset.name}'" : $"{fieldName}[{i}]";
                    Debug.LogWarning($"Profile {name} has no pivot or landing asset assigned - ignoring it.", context);
                    continue;
                }

                profiles.Add(profile);
            }

            return profiles.ToArray();
        }
    }
}
