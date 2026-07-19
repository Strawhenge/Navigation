using UnityEngine;

namespace Development
{
    public class CameraScript : MonoBehaviour
    {
        [SerializeField] Transform _target;
        [SerializeField] Vector3 _positionOffset = new(0, 1, -2);
        [SerializeField, Min(0f)] float _followSmoothTime = 0.15f;

        Vector3 _followVelocity;

        void LateUpdate()
        {
            if (_target == null)
                return;

            var desiredPosition = _target.position + _positionOffset;

            if (_followSmoothTime <= 0f)
            {
                transform.position = desiredPosition;
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _followVelocity,
                _followSmoothTime);
        }
    }
}