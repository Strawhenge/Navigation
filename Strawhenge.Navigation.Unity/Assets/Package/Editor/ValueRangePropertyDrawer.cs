using UnityEditor;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Editor
{
    // Draws a ValueRange as Min and Max side by side on one line, with a warning underneath if max < min.
    [CustomPropertyDrawer(typeof(ValueRange))]
    public class ValueRangePropertyDrawer : PropertyDrawer
    {
        const float Spacing = 4f;
        const float FieldLabelWidth = 28f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;

            return IsInvalid(property)
                ? lineHeight + EditorGUIUtility.standardVerticalSpacing + lineHeight * 2
                : lineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var min = property.FindPropertyRelative(ValueRange.MinFieldName);
            var max = property.FindPropertyRelative(ValueRange.MaxFieldName);

            EditorGUI.BeginProperty(position, label, property);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var fields = EditorGUI.PrefixLabel(line, label);

            var indentLevel = EditorGUI.indentLevel;
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = FieldLabelWidth;

            var halfWidth = (fields.width - Spacing) / 2f;
            EditorGUI.PropertyField(new Rect(fields.x, fields.y, halfWidth, fields.height), min, new GUIContent("Min"));
            EditorGUI.PropertyField(
                new Rect(fields.x + halfWidth + Spacing, fields.y, halfWidth, fields.height),
                max,
                new GUIContent("Max"));

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indentLevel;

            if (IsInvalid(property))
            {
                var warning = new Rect(
                    fields.x,
                    line.yMax + EditorGUIUtility.standardVerticalSpacing,
                    position.xMax - fields.x,
                    EditorGUIUtility.singleLineHeight * 2);

                EditorGUI.HelpBox(warning, "Max is less than Min, so this range never matches.", MessageType.Warning);
            }

            EditorGUI.EndProperty();
        }

        static bool IsInvalid(SerializedProperty property) =>
            property.FindPropertyRelative(ValueRange.MaxFieldName).floatValue <
            property.FindPropertyRelative(ValueRange.MinFieldName).floatValue;
    }
}
