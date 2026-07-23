using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Editor
{
    // TODO Move this to Common.Editor
    public class PrivateFieldInspector<TScript> where TScript : MonoBehaviour
    {
        readonly TScript _target;
        readonly FieldInfo[] _fields;
        bool _show;

        public PrivateFieldInspector(TScript target)
        {
            _target = target;
            _fields = typeof(TScript)
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
                EditorGUI.BeginDisabledGroup(true);
                
                foreach (var field in _fields)
                    EditorGUILayout.TextField(
                        $"{field.Name}",
                        field.GetValue(_target)?.ToString());
                
                EditorGUI.EndDisabledGroup();
            }
        }
    }
}