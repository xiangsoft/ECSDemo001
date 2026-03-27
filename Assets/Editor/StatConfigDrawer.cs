using UnityEditor;
using UnityEngine;
using Xiangsoft.Lib.ECS.Attribute;

namespace Xiangsoft.Lib.EditorTools
{
    [CustomPropertyDrawer(typeof(FloatStatConfig))]
    [CustomPropertyDrawer(typeof(IntStatConfig))]
    [CustomPropertyDrawer(typeof(LongStatConfig))]
    [CustomPropertyDrawer(typeof(StringStatConfig))]
    public class StatConfigDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 找到结构体里的 "Type" 字段
            SerializedProperty typeProp = property.FindPropertyRelative("Type");

            // 如果找到了，并且它是个枚举
            if (typeProp != null && typeProp.propertyType == SerializedPropertyType.Enum)
            {
                int index = typeProp.enumValueIndex;
                // 获取当前枚举的显示名称，并替换掉默认的 "Element X"
                if (index >= 0 && index < typeProp.enumDisplayNames.Length)
                {
                    label.text = typeProp.enumDisplayNames[index];
                }
            }

            // 使用替换后的名字，正常绘制原本的折叠面板和内容
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 保持原有的高度计算方式
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}