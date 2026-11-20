using Strawhenge.Navigation.Unity;
using UnityEditor;
using UnityEngine;

namespace Development.Editor
{
    // POC: the same check as the package's RigidbodyMotorScriptEditor. Flags a character that can't turn during
    // pivots: an Animator with no RootMotionRelayScript beside it, with a button that adds one with its motor set.
    [CustomEditor(typeof(KccMotorScript))]
    public class KccMotorScriptEditor : UnityEditor.Editor
    {
        // RootMotionRelayScript.MotorFieldName is internal to the package, so the field is named here.
        const string RelayMotorFieldName = "_motor";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var motor = (KccMotorScript)target;
            var animator = motor.GetComponentInChildren<Animator>(includeInactive: true);

            if (animator == null || animator.GetComponent<RootMotionRelayScript>() != null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"Pivots won't turn this character without a {nameof(RootMotionRelayScript)} on the Animator's " +
                $"GameObject ('{animator.name}'). Characters that don't use pivots don't need one.",
                MessageType.Warning);

            if (GUILayout.Button($"Add {ObjectNames.NicifyVariableName(nameof(RootMotionRelayScript))}"))
            {
                AddRelay(animator.gameObject, motor);

                // The components just changed under this inspector: end this GUI pass rather than finish its layout.
                GUIUtility.ExitGUI();
            }
        }

        // Adds the relay with its motor set, as one undoable step.
        static void AddRelay(GameObject animatorObject, KccMotorScript motor)
        {
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Add {nameof(RootMotionRelayScript)}");

            var relay = Undo.AddComponent<RootMotionRelayScript>(animatorObject);

            var serializedRelay = new SerializedObject(relay);
            serializedRelay.FindProperty(RelayMotorFieldName).objectReferenceValue = motor;
            serializedRelay.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(undoGroup);
        }
    }
}
