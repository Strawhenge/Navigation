using Strawhenge.Navigation.Unity.Destination;
using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    sealed class FakeDestinationSettings : IDestinationSettings
    {
        public float JumpSpeed { get; set; } = DefaultDestinationSettings.JumpSpeed;
        public float FallHorizontalSpeed { get; set; } = DefaultDestinationSettings.FallHorizontalSpeed;
        public float FallAcceleration { get; set; } = DefaultDestinationSettings.FallAcceleration;
        public LayerMask GroundLayers { get; set; } = DefaultDestinationSettings.GroundLayers;
        public float ApproximateSearchDistance { get; set; } = DefaultDestinationSettings.ApproximateSearchDistance;
        public IPivotProfile[] StationaryPivots { get; set; } = Array.Empty<IPivotProfile>();
        public IPivotProfile[] MovingPivots { get; set; } = Array.Empty<IPivotProfile>();
        public ILandingProfile[] JumpLandings { get; set; } = Array.Empty<ILandingProfile>();
        public ILandingProfile[] FallLandings { get; set; } = Array.Empty<ILandingProfile>();
        public bool DeferJumpTrigger { get; set; }
        public float AnimatorWaitTimeout { get; set; } = DefaultDestinationSettings.AnimatorWaitTimeout;
    }
}
