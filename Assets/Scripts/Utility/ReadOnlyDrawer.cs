using UnityEngine;
using UnityEditor;

/// <summary>
/// [ReadOnly] Attribute를 사용하기 위한 코드
/// </summary>
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false; // 입력 비활성화
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
