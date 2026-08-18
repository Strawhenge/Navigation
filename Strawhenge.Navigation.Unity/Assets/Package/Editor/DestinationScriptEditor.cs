using Strawhenge.Navigation.Unity.Destination;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Editor
{
    [CustomEditor(typeof(DestinationScript))]
    public class DestinationScriptEditor : UnityEditor.Editor
    {
        DestinationScript _target;
        Vector3 _location;
        float _speed = 1;
        bool _leisurely;
        bool _strafe;
        string _info;

        void OnEnable()
        {
            _target = _target ??= target as DestinationScript;
            _location = _target!.transform.position;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Destination Controller", EditorStyles.boldLabel);

            _location = EditorGUILayout.Vector3Field("Location", _location);
            if (GUILayout.Button("Set Current Location"))
            {
                _location = _target.transform.position;
            }

            _speed = EditorGUILayout.FloatField("Speed", _speed);
            _leisurely = EditorGUILayout.Toggle("Leisurely", _leisurely);
            _strafe = EditorGUILayout.Toggle("Strafe", _strafe);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            EditorGUILayout.Separator();

            if (GUILayout.Button(nameof(DestinationControllerExtensions.GoToExactly)))
            {
                _info = null;
                _target.DestinationController.GoToExactly(_location, _speed, Callback, _leisurely, _strafe);
            }

            if (GUILayout.Button(nameof(DestinationControllerExtensions.GoToApproximately)))
            {
                _info = null;
                _target.DestinationController.GoToApproximately(_location, _speed, Callback, _leisurely, _strafe);
            }

            if (GUILayout.Button(nameof(DestinationController.IsLocationAccessible)))
            {
                _info = _target.DestinationController.IsLocationAccessible(_location).ToString();
            }

            if (GUILayout.Button(nameof(DestinationController.Cancel)))
            {
                _target.DestinationController.Cancel();
            }

            EditorGUI.EndDisabledGroup();

            if (_info != null)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.HelpBox(_info, MessageType.Info);
            }
        }

        void Callback(DestinationResult result)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{nameof(result.IsAtDestination)}: {result.IsAtDestination}");
            stringBuilder.AppendLine($"{nameof(result.IsDestinationInaccessible)}: {result.IsDestinationInaccessible}");
            stringBuilder.AppendLine($"{nameof(result.IsCancelled)}: {result.IsCancelled}");
            stringBuilder.AppendLine($"{nameof(result.HasNewDestination)}: {result.HasNewDestination}");

            _info = stringBuilder.ToString();
        }
    }
}