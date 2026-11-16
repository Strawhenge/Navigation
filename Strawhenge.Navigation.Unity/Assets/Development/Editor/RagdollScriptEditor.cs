using UnityEditor;
using UnityEngine;

namespace Development.Editor
{
    [CustomEditor(typeof(RagdollScript))]
    public class RagdollScriptEditor : UnityEditor.Editor
    {
        // Keeps the state shown up to date while playing.
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var ragdoll = (RagdollScript)target;

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Ragdoll", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            EditorGUILayout.LabelField("Is Ragdolled", Application.isPlaying ? ragdoll.IsRagdolled.ToString() : "-");

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(ragdoll.IsRagdolled);
            if (GUILayout.Button("Ragdoll On"))
                ragdoll.RagdollOn();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!ragdoll.IsRagdolled);
            if (GUILayout.Button("Ragdoll Off"))
                ragdoll.RagdollOff();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
        }
    }
}
