using UnityEngine;

namespace Development
{
    public class CameraScript : MonoBehaviour
    {
        [SerializeField] Transform _target;
        [SerializeField] Vector3 _positionOffset = new(0, 1, -2);
        [SerializeField] bool _enableOrbit = true;
        [SerializeField, Min(0f)] float _orbitDegreesPerSecond = 60f;
        [SerializeField, Min(0f)] float _followSmoothTime = 0.15f;

        Vector3 _followVelocity;

        void LateUpdate()
        {
            if (_target == null)
                return;

            var desiredPosition = _enableOrbit ? GetOrbitToBackPosition() : _target.position + _positionOffset;

            if (_followSmoothTime <= 0f)
            {
                transform.position = desiredPosition;
                transform.LookAt(_target.position);
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _followVelocity,
                _followSmoothTime);

            transform.LookAt(_target.position);
        }

        Vector3 GetOrbitToBackPosition()
        {
            var radius = new Vector2(_positionOffset.x, _positionOffset.z).magnitude;
            if (radius <= 0.0001f)
                return _target.position + Vector3.up * _positionOffset.y;

            var targetBack = Vector3.ProjectOnPlane(-_target.forward, Vector3.up).normalized;
            if (targetBack.sqrMagnitude <= 0.0001f)
                targetBack = Vector3.back;

            var fromTarget = Vector3.ProjectOnPlane(transform.position - _target.position, Vector3.up);
            var currentDir = fromTarget.sqrMagnitude > 0.0001f
                ? fromTarget.normalized
                : Vector3.ProjectOnPlane(_positionOffset, Vector3.up).normalized;

            if (currentDir.sqrMagnitude <= 0.0001f)
                currentDir = targetBack;

            var maxRadians = _orbitDegreesPerSecond * Mathf.Deg2Rad * Time.deltaTime;
            var nextDir = Vector3.RotateTowards(currentDir, targetBack, maxRadians, 0f);
            var orbitOffset = nextDir * radius + Vector3.up * _positionOffset.y;

            return _target.position + orbitOffset;
        }
    }
}