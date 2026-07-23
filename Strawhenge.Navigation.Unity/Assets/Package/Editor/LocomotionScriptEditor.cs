using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Strawhenge.Navigation.Unity.Editor
{
    [CustomEditor(typeof(LocomotionScript))]
    public class LocomotionScriptEditor : UnityEditor.Editor
    {
        PrivateFieldInspector<LocomotionScript> _privateFieldInspector;

        void OnEnable()
        {
            EditorApplication.update += Update;
            _privateFieldInspector ??= new PrivateFieldInspector<LocomotionScript>(target as LocomotionScript);
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            _privateFieldInspector?.Inspect();
        }

        void Update()
        {
            if (_privateFieldInspector?.IsShowing ?? false)
                Repaint();
        }
    }
}