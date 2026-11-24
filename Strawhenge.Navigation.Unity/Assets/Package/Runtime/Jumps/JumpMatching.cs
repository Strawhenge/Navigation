using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Picks the jump to play when a jump starts, for locomotion and destination alike. Nothing allocates.
    static class JumpMatching
    {
        // Below this horizontal speed (or distance, for a link) there's no direction to measure, so it counts
        // as forward (0°).
        const float MinDirectionMagnitude = 0.1f;

        // The first matching profile's ID: from strafeJumps while strafing, else from jumps. False when none
        // matches, and the character should jump straight away.
        public static bool TryMatch(
            IJumpProfile[] jumps,
            IStrafeJumpProfile[] strafeJumps,
            bool strafe,
            float speed,
            float direction,
            out int jumpId)
        {
            if (strafe)
            {
                foreach (var jump in strafeJumps)
                {
                    if (jump.Matches(speed, direction) && jump.Id is int strafeJumpId)
                    {
                        jumpId = strafeJumpId;
                        return true;
                    }
                }
            }
            else
            {
                foreach (var jump in jumps)
                {
                    if (jump.Matches(speed) && jump.Id is int id)
                    {
                        jumpId = id;
                        return true;
                    }
                }
            }

            jumpId = default;
            return false;
        }

        // The signed angle in degrees from facing to movement, both flattened: 0 forward, 90 right, -90 left,
        // ±180 back. 0 when there's too little movement to have a direction.
        public static float GetDirection(Quaternion facing, Vector3 movement)
        {
            movement.y = 0f;

            if (movement.sqrMagnitude < MinDirectionMagnitude * MinDirectionMagnitude)
                return 0f;

            var forward = facing * Vector3.forward;
            forward.y = 0f;

            return Vector3.SignedAngle(forward, movement, Vector3.up);
        }
    }
}
