using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

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
                () => IsRunning(locomotion) ? locomotion.LocomotionController : null);
        }

        // Reading LocomotionController creates it if Awake hasn't yet (e.g. on an inactive GameObject), which Awake then
        // keeps. Never for a prefab asset or an object in Prefab Mode: no scripts run there, even in play mode.
        static bool IsRunning(LocomotionScript locomotion) =>
            Application.isPlaying &&
            locomotion != null &&
            !EditorUtility.IsPersistent(locomotion) &&
            !EditorSceneManager.IsPreviewScene(locomotion.gameObject.scene);

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