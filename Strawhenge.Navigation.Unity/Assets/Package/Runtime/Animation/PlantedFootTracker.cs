using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Works out which foot is planted from how fast each foot moves over the ground, whatever the clip: a planted
    // foot stays almost still while the other swings past at about twice the character's speed. Through a run's
    // flight phase it's the foot about to land. It only changes feet when the other foot is slower by more than
    // the margin, so standing still (both feet still) keeps the foot that last landed, and noise around a
    // changeover doesn't flicker between them.
    class PlantedFootTracker
    {
        readonly float _margin;

        Vector3 _lastLeftFoot;
        Vector3 _lastRightFoot;
        bool _hasLastFeet;

        internal PlantedFootTracker(float margin)
        {
            _margin = margin;
        }

        internal bool IsRightFootPlanted { get; private set; }

        // Called once per frame, after the Animator has posed the feet, with their world positions.
        internal void Update(Vector3 leftFoot, Vector3 rightFoot, float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            if (_hasLastFeet)
            {
                var leftSpeed = HorizontalSpeed(leftFoot - _lastLeftFoot, deltaTime);
                var rightSpeed = HorizontalSpeed(rightFoot - _lastRightFoot, deltaTime);

                if (rightSpeed + _margin < leftSpeed)
                    IsRightFootPlanted = true;
                else if (leftSpeed + _margin < rightSpeed)
                    IsRightFootPlanted = false;
            }

            _lastLeftFoot = leftFoot;
            _lastRightFoot = rightFoot;
            _hasLastFeet = true;
        }

        // The feet may have jumped since the last update (the Animator was off, or the character was moved), so
        // the next update only records where they are. Keeps the foot that was planted.
        internal void Reset() => _hasLastFeet = false;

        static float HorizontalSpeed(Vector3 movement, float deltaTime)
        {
            movement.y = 0f;
            return movement.magnitude / deltaTime;
        }
    }
}
