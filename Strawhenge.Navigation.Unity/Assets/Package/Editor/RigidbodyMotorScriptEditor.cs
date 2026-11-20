using UnityEditor;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Editor
{
    // Flags a character that can't turn during pivots: an Animator with no RootMotionRelayScript beside it. Shown
    // here rather than logged, so setups without pivots (and tests) aren't warned.
    [CustomEditor(typeof(RigidbodyMotorScript))]
    public class RigidbodyMotorScriptEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var motor = (RigidbodyMotorScript)target;
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
        static void AddRelay(GameObject animatorObject, RigidbodyMotorScript motor)
        {
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Add {nameof(RootMotionRelayScript)}");

            var relay = Undo.AddComponent<RootMotionRelayScript>(animatorObject);

            var serializedRelay = new SerializedObject(relay);
            serializedRelay.FindProperty(RootMotionRelayScript.MotorFieldName).objectReferenceValue = motor;
            serializedRelay.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(undoGroup);
        }
    }
}
