using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Editor
{
    // TODO Move this to Common.Editor
    public class PrivateFieldInspector<TTarget> where TTarget : class
    {
        readonly Func<TTarget> _getTarget;
        readonly FieldInfo[] _fields;
        bool _show;

        public PrivateFieldInspector(TTarget target) : this(() => target)
        {
        }

        // For a target that may not exist yet (e.g. created in play mode): read each time it's drawn.
        public PrivateFieldInspector(Func<TTarget> getTarget)
        {
            _getTarget = getTarget;
            _fields = typeof(TTarget)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(field => !field.IsDefined(typeof(SerializeField), inherit: false))
                .ToArray();
        }
        
        public bool IsShowing => _show;

        public void Inspect()
        {
            _show = EditorGUILayout.Foldout(_show, "Private Fields");

            if (_show)
            {
                var target = _getTarget();

                if (target == null)
                {
                    EditorGUILayout.LabelField("Shown in play mode.");
                    return;
                }

                EditorGUI.BeginDisabledGroup(true);
                
                foreach (var field in _fields)
                    EditorGUILayout.TextField(
                        $"{field.Name}",
                        field.GetValue(target)?.ToString());
                
                EditorGUI.EndDisabledGroup();
            }
        }
    }
}
