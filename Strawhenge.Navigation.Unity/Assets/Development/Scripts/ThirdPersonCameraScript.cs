using Strawhenge.Common.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using ILogger = Strawhenge.Common.Logging.ILogger;

namespace Development
{
    // Orbits a target with the mouse (Look, a per-frame delta) or a stick (Look Rate, held), and zooms with the
    // mouse wheel (Zoom, a per-frame delta) or the d-pad (Zoom Rate, held). Mouse look waits for a locked cursor;
    // the stick and zoom always work, so a controller never loses the camera after Escape.
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
        [SerializeField] float _minPitch = -35f;
        [SerializeField] float _maxPitch = 70f;

        [Header("Mouse")]
        [SerializeField] float _lookSensitivity = 0.15f;
        [SerializeField] bool _invertY;
        [SerializeField] float _zoomSpeed = 0.005f;

        [Header("Stick")]
        [Tooltip("Degrees per second with the stick pushed all the way.")]
        [SerializeField] float _lookRateSpeed = 180f;
        [SerializeField] bool _invertLookRateY;
        [Tooltip("Metres per second with the d-pad held.")]
        [SerializeField] float _zoomRateSpeed = 4f;

        [Header("Collision")]
        [SerializeField] LayerMask _collisionLayers = ~0;
        [SerializeField] float _collisionRadius = 0.2f;
        [SerializeField] float _collisionReturnSmoothTime = 0.25f;

        [Header("Cursor")]
        [SerializeField] bool _lockCursor = true;

        // Optional: without one, warnings go through a UnityLogger on this GameObject.
        [SerializeField] LoggerScript _logger;

        readonly RaycastHit[] _hits = new RaycastHit[MaxCollisionHits];

        Vector2 _lookInput;
        Vector2 _lookRateInput;
        float _zoomInput;
        float _zoomRateInput;
        float _yaw;
        float _pitch;
        Vector3 _pivot;
        Vector3 _pivotVelocity;
        float _currentDistance;
        float _distanceVelocity;

        bool IsCursorLocked => !_lockCursor || Cursor.lockState == CursorLockMode.Locked;

        // Mouse delta: accumulated per frame, so it's applied once and then consumed.
        public void Look(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }

        // Stick: a rate, held until it changes (callbacks only come on a change).
        public void LookRate(InputAction.CallbackContext context)
        {
            _lookRateInput = context.ReadValue<Vector2>();
        }

        // Mouse wheel: a per-frame delta, like Look.
        public void Zoom(InputAction.CallbackContext context)
        {
            _zoomInput = context.ReadValue<float>();
        }

        // D-pad: a rate, like LookRate.
        public void ZoomRate(InputAction.CallbackContext context)
        {
            _zoomRateInput = context.ReadValue<float>();
        }

        // Snaps to the new target rather than gliding across the scene; the orbit (yaw, pitch, zoom) is kept.
        public void SetTarget(Transform target)
        {
            _target = target;

            if (_target == null)
                return;

            _pivot = GetTargetPivot();
            _pivotVelocity = Vector3.zero;

            // Start turns the camera off when it has no target.
            enabled = true;
        }

        void Start()
        {
            if (_target == null)
            {
                GetLogger().LogWarning("Camera has no target.");
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
                ApplyMouseLook();

            ApplyStickLook(Time.deltaTime);
            ApplyZoom(Time.deltaTime);

            // The deltas are consumed so they're never applied twice; the rates are held.
            _lookInput = Vector2.zero;
            _zoomInput = 0f;

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

        void ApplyMouseLook() => Rotate(_lookInput * _lookSensitivity, _invertY);

        void ApplyStickLook(float deltaTime) => Rotate(_lookRateInput * (_lookRateSpeed * deltaTime), _invertLookRateY);

        // Up (positive y) looks up, unless inverted.
        void Rotate(Vector2 degrees, bool invertY)
        {
            _yaw = Mathf.Repeat(_yaw + degrees.x, 360f);
            _pitch = Mathf.Clamp(_pitch + (invertY ? degrees.y : -degrees.y), _minPitch, _maxPitch);
        }

        void ApplyZoom(float deltaTime)
        {
            var change = _zoomInput * _zoomSpeed + _zoomRateInput * _zoomRateSpeed * deltaTime;

            if (change != 0f)
                _distance = Mathf.Clamp(_distance - change, _minDistance, _maxDistance);
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

        ILogger GetLogger() => _logger != null ? _logger.Logger : new UnityLogger(gameObject);

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
