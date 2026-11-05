using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    sealed class DefaultDestinationSettings : IDestinationSettings
    {
        internal const float JumpSpeed = 4;
        internal const float FallHorizontalSpeed = 5;
        internal const float FallAcceleration = 9.81f;
        internal const int GroundLayers = Physics.DefaultRaycastLayers;

        public static IDestinationSettings Instance { get; } = new DefaultDestinationSettings();

        DefaultDestinationSettings()
        {
        }

        float IDestinationSettings.JumpSpeed => JumpSpeed;

        float IDestinationSettings.FallHorizontalSpeed => FallHorizontalSpeed;

        float IDestinationSettings.FallAcceleration => FallAcceleration;

        LayerMask IDestinationSettings.GroundLayers => GroundLayers;
    }
}
