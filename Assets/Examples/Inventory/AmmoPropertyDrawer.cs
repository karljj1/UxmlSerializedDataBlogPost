#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(Ammo))]
public class AmmoPropertyDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = new VisualElement { style = { flexDirection = FlexDirection.Row } };

        var count = property.FindPropertyRelative("count");
        var maxCount = property.FindPropertyRelative("maxCount");

        var ammoField = new IntegerField("Ammo") { isDelayed = true, bindingPath = count.propertyPath };
        ammoField.RegisterValueChangedCallback(e =>
        {
            count.intValue = Mathf.Min(maxCount.intValue, e.newValue);
            property.serializedObject.ApplyModifiedProperties();
        });
        root.Add(ammoField);
        root.Add(new Label("/"));

        var countField = new IntegerField { isDelayed = true, bindingPath = property.FindPropertyRelative("maxCount").propertyPath };
        countField.RegisterValueChangedCallback(e =>
        {
            count.intValue = Mathf.Min(e.newValue, count.intValue);
            property.serializedObject.ApplyModifiedProperties();
        });
        root.Add(countField);

        root.Bind(property.serializedObject);

        return root;
    }
}

#endif