using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CategoryAttribute))]
public sealed class CategoryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType != SerializedPropertyType.Enum)
        {
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.EndProperty();
            return;
        }

        if (property.name == nameof(ItemData.MainCategory))
        {
            DrawMainCategory(position, property, label);
        }
        else if (property.name == nameof(ItemData.MiddleCategory))
        {
            DrawMiddleCategory(position, property, label);
        }
        else if (property.name == nameof(ItemData.SubCategory))
        {
            DrawSubCategory(position, property, label);
        }
        else
        {
            EditorGUI.PropertyField(position, property, label);
        }

        EditorGUI.EndProperty();
    }

    private static void DrawMainCategory(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(position, property, label);

        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }

        SerializedProperty middleProperty = FindSiblingProperty(property, nameof(ItemData.MiddleCategory));
        SerializedProperty subProperty = FindSiblingProperty(property, nameof(ItemData.SubCategory));

        if (middleProperty == null || subProperty == null)
        {
            return;
        }

        ItemMainCategory selectedMain = (ItemMainCategory)property.enumValueIndex;
        EnsureValidMiddleCategory(middleProperty, selectedMain);
        EnsureValidSubCategory(subProperty, (ItemMiddleCategory)middleProperty.enumValueIndex);
    }

    private static void DrawMiddleCategory(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty mainProperty = FindSiblingProperty(property, nameof(ItemData.MainCategory));
        SerializedProperty subProperty = FindSiblingProperty(property, nameof(ItemData.SubCategory));

        ItemMainCategory selectedMain = mainProperty != null
            ? (ItemMainCategory)mainProperty.enumValueIndex
            : ItemMainCategory.None;

        ItemMiddleCategory[] allowedValues = GetAllowedMiddleCategories(selectedMain);
        DrawFilteredEnumPopup(position, property, label, allowedValues);

        if (subProperty != null)
        {
            EnsureValidSubCategory(subProperty, (ItemMiddleCategory)property.enumValueIndex);
        }
    }

    private static void DrawSubCategory(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty middleProperty = FindSiblingProperty(property, nameof(ItemData.MiddleCategory));

        ItemMiddleCategory selectedMiddle = middleProperty != null
            ? (ItemMiddleCategory)middleProperty.enumValueIndex
            : ItemMiddleCategory.None;

        ItemSubCategory[] allowedValues = GetAllowedSubCategories(selectedMiddle);
        DrawFilteredEnumPopup(position, property, label, allowedValues);
    }

    private static void EnsureValidMiddleCategory(SerializedProperty middleProperty, ItemMainCategory mainCategory)
    {
        ItemMiddleCategory currentMiddle = (ItemMiddleCategory)middleProperty.enumValueIndex;
        ItemMiddleCategory[] allowedValues = GetAllowedMiddleCategories(mainCategory);

        if (!allowedValues.Contains(currentMiddle))
        {
            middleProperty.enumValueIndex = (int)ItemMiddleCategory.None;
        }
    }

    private static void EnsureValidSubCategory(SerializedProperty subProperty, ItemMiddleCategory middleCategory)
    {
        ItemSubCategory currentSub = (ItemSubCategory)subProperty.enumValueIndex;
        ItemSubCategory[] allowedValues = GetAllowedSubCategories(middleCategory);

        if (!allowedValues.Contains(currentSub))
        {
            subProperty.enumValueIndex = (int)ItemSubCategory.None;
        }
    }

    private static void DrawFilteredEnumPopup<TEnum>(Rect position, SerializedProperty property, GUIContent label, TEnum[] allowedValues)
        where TEnum : Enum
    {
        if (allowedValues == null || allowedValues.Length == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        TEnum currentValue = (TEnum)Enum.ToObject(typeof(TEnum), property.enumValueIndex);
        int selectedIndex = Array.IndexOf(allowedValues, currentValue);

        if (selectedIndex < 0)
        {
            selectedIndex = 0;
            property.enumValueIndex = Convert.ToInt32(allowedValues[0]);
        }

        string[] displayedOptions = allowedValues.Select(value => value.ToString()).ToArray();
        int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, displayedOptions);
        property.enumValueIndex = Convert.ToInt32(allowedValues[Mathf.Clamp(newIndex, 0, allowedValues.Length - 1)]);
    }

    private static ItemMiddleCategory[] GetAllowedMiddleCategories(ItemMainCategory mainCategory)
    {
        return CategoryDefinitionMaps.MainToMiddleMap.TryGetValue(mainCategory, out ItemMiddleCategory[] allowedValues)
            ? allowedValues
            : new[] { ItemMiddleCategory.None };
    }

    private static ItemSubCategory[] GetAllowedSubCategories(ItemMiddleCategory middleCategory)
    {
        return CategoryDefinitionMaps.MiddleToSubMap.TryGetValue(middleCategory, out ItemSubCategory[] allowedValues)
            ? allowedValues
            : new[] { ItemSubCategory.None };
    }

    private static SerializedProperty FindSiblingProperty(SerializedProperty property, string siblingName)
    {
        string propertyPath = property.propertyPath;
        int lastDotIndex = propertyPath.LastIndexOf('.');
        string siblingPath = lastDotIndex >= 0
            ? $"{propertyPath.Substring(0, lastDotIndex)}.{siblingName}"
            : siblingName;

        return property.serializedObject.FindProperty(siblingPath);
    }
}
