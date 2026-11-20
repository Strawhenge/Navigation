using Strawhenge.Navigation.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Development
{
    // Switches the controls and camera between characters (one per motor, say). Bind the PlayerInput's
    // Switch Character action (Tab) to Switch.
    public class CharacterSwitcherScript : MonoBehaviour
    {
        [SerializeField] LocomotionScript[] _characters;
        [SerializeField] ControlsScript _controls;
        [SerializeField] ThirdPersonCameraScript _camera;
        [SerializeField] bool _showLabel = true;

        int _index;

        // In Awake, so the camera has its target before its Start checks for one.
        void Awake()
        {
            if (_controls == null)
                _controls = FindObjectOfType<ControlsScript>();

            if (_camera == null)
                _camera = FindObjectOfType<ThirdPersonCameraScript>();

            if (_characters == null || _characters.Length == 0)
            {
                Debug.LogWarning($"No '{nameof(_characters)}' to switch between.", this);
                enabled = false;
                return;
            }

            Select(0);
        }

        public void Switch(InputAction.CallbackContext context)
        {
            if (!context.performed || !enabled)
                return;

            Select((_index + 1) % _characters.Length);
        }

        void Select(int index)
        {
            _index = index;
            var character = _characters[index];

            if (_controls != null)
                _controls.SetPlayer(character);

            if (_camera != null)
                _camera.SetTarget(character.transform);
        }

        void OnGUI()
        {
            if (!_showLabel || _characters == null || _characters.Length == 0)
                return;

            var character = _characters[_index];
            var label = character != null
                ? $"{character.name} ({_index + 1}/{_characters.Length})"
                : "(missing character)";

            GUI.Label(new Rect(10, 10, 500, 25), label);
        }
    }
}
