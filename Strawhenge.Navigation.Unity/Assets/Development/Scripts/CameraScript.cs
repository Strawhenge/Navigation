using UnityEngine;
using UnityEngine.InputSystem;

namespace Development
{
    public class CameraScript : MonoBehaviour
    {
        [SerializeField] Transform _target;
        [SerializeField] float _distanceFromTarget = 3f;
        [SerializeField] float _heightAboveTarget = 1f;
        [SerializeField] float _rotationSpeed = 90f;

        float _input;
        float _currentAngle;

        void Update()
        {
            if (_target == null)
                return;

            _currentAngle += _rotationSpeed * _input * Time.deltaTime;
            var offset = Quaternion.Euler(0f, _currentAngle, 0f) * new Vector3(0f, 0f, -_distanceFromTarget);
            var targetPosition = _target.position + new Vector3(0f, _heightAboveTarget, 0f);

            transform.position = targetPosition + offset;
            transform.LookAt(targetPosition);
        }

        public void Rotate(InputAction.CallbackContext context)
        {
            _input = context.ReadValue<Vector2>().x;
        }
    }
}