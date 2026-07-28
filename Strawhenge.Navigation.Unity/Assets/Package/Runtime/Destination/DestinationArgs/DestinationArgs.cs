using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    public partial class DestinationArgs
    {
        public static IDestinationArgsBuilder GoToExactly(Vector3 location) => new ArgsBuilder(location, true);

        public static IDestinationArgsBuilder GoToApproximately(Vector3 location) => new ArgsBuilder(location, false);

        internal DestinationArgs(
            Vector3 location,
            float speed,
            bool locationMustBeExact,
            bool leisurely,
            Action<DestinationResult> callback)
        {
            Location = location;
            Speed = speed;
            LocationMustBeExact = locationMustBeExact;
            Leisurely = leisurely;
            Callback = callback;
        }

        DestinationArgs()
        {
        }

        internal Vector3 Location { get; private set; }

        internal Vector3? StartingLocation { get; private set; }

        internal float Speed { get; private set; }

        internal float? TargetDistance { get; private set; }

        internal bool LocationMustBeExact { get; private set; }

        internal bool Leisurely { get; private set; }

        internal Action<DestinationResult> Callback { get; private set; }
    }
}