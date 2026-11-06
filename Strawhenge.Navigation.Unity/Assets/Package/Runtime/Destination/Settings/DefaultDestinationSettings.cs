using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    sealed class DefaultDestinationSettings : IDestinationSettings
    {
        internal const float JumpSpeed = 4;
        internal const float FallHorizontalSpeed = 5;
        internal const float FallAcceleration = 9.81f;
        internal const int GroundLayers = Physics.DefaultRaycastLayers;
        internal const float ApproximateSearchDistance = 5;
        internal const float AnimatorWaitTimeout = 2;

        public static IDestinationSettings Instance { get; } = new DefaultDestinationSettings();

        DefaultDestinationSettings()
        {
        }

        float IDestinationSettings.JumpSpeed => JumpSpeed;

        float IDestinationSettings.FallHorizontalSpeed => FallHorizontalSpeed;

        float IDestinationSettings.FallAcceleration => FallAcceleration;

        LayerMask IDestinationSettings.GroundLayers => GroundLayers;

        float IDestinationSettings.ApproximateSearchDistance => ApproximateSearchDistance;

        IPivotProfile[] IDestinationSettings.StationaryPivots => Array.Empty<IPivotProfile>();

        IPivotProfile[] IDestinationSettings.MovingPivots => Array.Empty<IPivotProfile>();

        ILandingProfile[] IDestinationSettings.JumpLandings => Array.Empty<ILandingProfile>();

        ILandingProfile[] IDestinationSettings.FallLandings => Array.Empty<ILandingProfile>();

        bool IDestinationSettings.DeferJumpTrigger => false;

        float IDestinationSettings.AnimatorWaitTimeout => AnimatorWaitTimeout;
    }
}
