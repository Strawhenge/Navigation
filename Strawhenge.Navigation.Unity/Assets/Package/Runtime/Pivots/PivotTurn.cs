using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Whether a completed pivot turned the character. Measured from its rotation before and after, as root
    // motion plays the turn; a pivot that didn't turn it would otherwise match again straight away.
    static class PivotTurn
    {
        // Less than this counts as not turning: root motion can drift a little even without a turn.
        const float MinAngle = 1f;

        public static bool DidTurn(Quaternion from, Quaternion to) => Quaternion.Angle(from, to) >= MinAngle;
    }
}
