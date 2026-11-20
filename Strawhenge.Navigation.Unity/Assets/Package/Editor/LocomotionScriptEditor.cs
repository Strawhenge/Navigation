using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Strawhenge.Navigation.Unity.Editor
{
    [CustomEditor(typeof(LocomotionScript))]
    public class LocomotionScriptEditor : UnityEditor.Editor
    {
        PrivateFieldInspector<LocomotionController> _privateFieldInspector;

        void OnEnable()
        {
            EditorApplication.update += Update;
            var locomotion = (LocomotionScript)target;

            // The logic and its state live in the controller.
            _privateFieldInspector ??= new PrivateFieldInspector<LocomotionController>(
                () => locomotion != null ? locomotion.ControllerIfCreated : null);
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