using Strawhenge.Common.Ranges;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Shared by the locomotion test classes.
    static class LocomotionTestCharacter
    {
        // Builds a character with a CharacterController and LocomotionScript. The default controller is
        // 2 m tall and centred on the transform, so its feet are 1 m below position. It is built
        // inactive so the settings are in place before Awake runs. Adds it to created for tear-down.
        public static LocomotionScript Create(
            ILocomotionSettings settings,
            Vector3 position,
            ICollection<Object> created,
            Quaternion? rotation = null)
        {
            var character = new GameObject("Character");
            character.SetActive(false);
            created.Add(character);

            character.transform.SetPositionAndRotation(position, rotation ?? Quaternion.identity);
            var characterController = character.AddComponent<CharacterController>();

            var locomotion = character.AddComponent<LocomotionScript>();
            locomotion.SetCharacterController(characterController);
            locomotion.SetSettings(settings);

            character.SetActive(true);
            return locomotion;
        }
    }

    sealed class FakeLocomotionSettings : ILocomotionSettings
    {
        public float WalkSpeed { get; set; } = DefaultLocomotionSettings.WalkSpeed;
        public float RunSpeed { get; set; } = DefaultLocomotionSettings.RunSpeed;
        public float SprintSpeed { get; set; } = DefaultLocomotionSettings.SprintSpeed;
        public float Acceleration { get; set; } = DefaultLocomotionSettings.Acceleration;
        public float Deceleration { get; set; } = DefaultLocomotionSettings.Deceleration;
        public float TurnSpeed { get; set; } = DefaultLocomotionSettings.TurnSpeed;
        public IPivotProfile[] StationaryPivots { get; set; } = Array.Empty<IPivotProfile>();
        public IPivotProfile[] MovingPivots { get; set; } = Array.Empty<IPivotProfile>();
        public float JumpHeight { get; set; } = DefaultLocomotionSettings.JumpHeight;
        public float CoyoteTime { get; set; } = DefaultLocomotionSettings.CoyoteTime;
        public bool DeferJumpTrigger { get; set; }
        public ILandingProfile[] JumpLandings { get; set; } = Array.Empty<ILandingProfile>();
        public float Gravity { get; set; } = DefaultLocomotionSettings.Gravity;
        public float GroundedGravity { get; set; } = DefaultLocomotionSettings.GroundedGravity;
        public float FallDistance { get; set; } = DefaultLocomotionSettings.FallDistance;
        public ILandingProfile[] FallLandings { get; set; } = Array.Empty<ILandingProfile>();
    }

    sealed class FakePivotProfile : IPivotProfile
    {
        public FakePivotProfile(int id, FloatRange speedRange, params FloatRange[] angleRanges)
        {
            Id = id;
            SpeedRange = speedRange;
            AngleRanges = angleRanges;
        }

        public int Id { get; }
        public FloatRange SpeedRange { get; }
        public IEnumerable<FloatRange> AngleRanges { get; }
    }

    sealed class FakeLandingProfile : ILandingProfile
    {
        public FakeLandingProfile(int id, FloatRange fallDistanceRange, FloatRange speedRange)
        {
            Id = id;
            FallDistanceRange = fallDistanceRange;
            SpeedRange = speedRange;
        }

        public int Id { get; }
        public FloatRange FallDistanceRange { get; }
        public FloatRange SpeedRange { get; }
    }
}
