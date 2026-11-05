using UnityEngine;
using UnityEngine.InputSystem;

namespace Development
{
    public class ThirdPersonCameraScript : MonoBehaviour
    {
        const int MaxCollisionHits = 16;

        [Header("Target")]
        [SerializeField] Transform _target;
        [SerializeField] float _pivotHeight = 1.5f;
        [SerializeField] float _followSmoothTime = 0.08f;

        [Header("Orbit")]
        [SerializeField] float _distance = 3f;
        [SerializeField] float _minDistance = 1f;
        [SerializeField] float _maxDistance = 6f;
        [SerializeField] float _zoomSpeed = 0.005f;
        [SerializeField] float _lookSensitivity = 0.15f;
        [SerializeField] float _minPitch = -35f;
        [SerializeField] float _maxPitch = 70f;
        [SerializeField] bool _invertY;

        [Header("Collision")]
        [SerializeField] LayerMask _collisionLayers = ~0;
        [SerializeField] float _collisionRadius = 0.2f;
        [SerializeField] float _collisionReturnSmoothTime = 0.25f;

        [Header("Cursor")]
        [SerializeField] bool _lockCursor = true;

        readonly RaycastHit[] _hits = new RaycastHit[MaxCollisionHits];

        Vector2 _lookInput;
        float _yaw;
        float _pitch;
        Vector3 _pivot;
        Vector3 _pivotVelocity;
        float _currentDistance;
        float _distanceVelocity;

        bool IsCursorLocked => !_lockCursor || Cursor.lockState == CursorLockMode.Locked;

        public void Look(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }

        void Start()
        {
            if (_target == null)
            {
                Debug.LogWarning("Camera has no target.", this);
                enabled = false;
                return;
            }

            var euler = transform.eulerAngles;
            _yaw = euler.y;
            _pitch = Mathf.Clamp(NormalizeAngle(euler.x), _minPitch, _maxPitch);
            _pivot = GetTargetPivot();
            _currentDistance = _distance;

            if (_lockCursor)
                LockCursor();
        }

        void OnDisable()
        {
            if (_lockCursor)
                UnlockCursor();
        }

        void Update()
        {
            if (!_lockCursor)
                return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                UnlockCursor();

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                LockCursor();
        }

        void LateUpdate()
        {
            if (IsCursorLocked)
            {
                ApplyLook();
                ApplyZoom();
            }

            // Mouse delta is already accumulated per frame; consume it so it's never applied twice.
            _lookInput = Vector2.zero;

            _pivot = Vector3.SmoothDamp(_pivot, GetTargetPivot(), ref _pivotVelocity, _followSmoothTime);

            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var direction = rotation * Vector3.back;

            var allowedDistance = GetUnobstructedDistance(direction, _distance);

            // Pull in instantly so the camera never clips through walls, ease back out when clear.
            _currentDistance = allowedDistance < _currentDistance
                ? allowedDistance
                : Mathf.SmoothDamp(_currentDistance, allowedDistance, ref _distanceVelocity, _collisionReturnSmoothTime);

            transform.SetPositionAndRotation(_pivot + direction * _currentDistance, rotation);
        }

        void ApplyLook()
        {
            _yaw = Mathf.Repeat(_yaw + _lookInput.x * _lookSensitivity, 360f);

            var pitchDelta = _lookInput.y * _lookSensitivity;
            _pitch = Mathf.Clamp(_pitch + (_invertY ? pitchDelta : -pitchDelta), _minPitch, _maxPitch);
        }

        void ApplyZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            var scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
                _distance = Mathf.Clamp(_distance - scroll * _zoomSpeed, _minDistance, _maxDistance);
        }

        float GetUnobstructedDistance(Vector3 direction, float desiredDistance)
        {
            var hitCount = Physics.SphereCastNonAlloc(
                _pivot,
                _collisionRadius,
                direction,
                _hits,
                desiredDistance,
                _collisionLayers,
                QueryTriggerInteraction.Ignore);

            var distance = desiredDistance;
            var targetRoot = _target.root;

            for (var i = 0; i < hitCount; i++)
            {
                var hit = _hits[i];

                // Overlapping at the start of the cast (distance 0), or part of the character itself.
                if (hit.distance <= 0f || hit.transform.IsChildOf(targetRoot))
                    continue;

                if (hit.distance < distance)
                    distance = hit.distance;
            }

            return Mathf.Max(distance, 0f);
        }

        Vector3 GetTargetPivot() => _target.position + Vector3.up * _pivotHeight;

        static float NormalizeAngle(float angle) => angle > 180f ? angle - 360f : angle;

        static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}