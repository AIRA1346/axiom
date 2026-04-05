using UnityEditor;
using UnityEngine;

namespace Ultrabolt.SkyEngine
{
    public class EditorMethods : MonoBehaviour
    {
        public static void DrawBoxGroup(GUIContent label, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            EditorGUILayout.BeginVertical();

            drawContent?.Invoke();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        public static void DrawBoxGroup(GUIContent label, System.Action drawContent, System.Action onButtonClick, string buttonText, Color buttonColor, float width = 20f)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            Color color = GUI.color;
            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button(buttonText, GUILayout.Width(width), GUILayout.Height(18)))
                onButtonClick?.Invoke();
            GUI.backgroundColor = color;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Indented content box
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            EditorGUILayout.BeginVertical();

            drawContent?.Invoke();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        public static void DrawFoldoutGroup(GUIContent label, ref bool foldout, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginVertical("box");
            foldout = DrawCustomFoldout(label, foldout);
            EditorGUILayout.EndVertical();
            if (foldout)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20f);
                EditorGUILayout.BeginVertical();

                drawContent?.Invoke();

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        public static void DrawFoldoutGroup(GUIContent label, ref bool foldout, System.Action drawContent, System.Action onButtonClick, string buttonText, Color buttonColor, float width = 20f)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            foldout = DrawCustomFoldout(label, foldout);
            Color color = GUI.color;
            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button(buttonText, GUILayout.Width(width), GUILayout.Height(18)))
                onButtonClick?.Invoke();
            GUI.backgroundColor = color;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if (foldout)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20f);
                EditorGUILayout.BeginVertical();

                drawContent?.Invoke();

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }


        private static bool DrawCustomFoldout(GUIContent iconLabel, bool foldout)
        {
            EditorGUILayout.BeginVertical("helpbox");
            Rect rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.IndentedRect(rect);

            Rect toggleRect = new(rect.x + 15f, rect.y, 18f, rect.height);
            Rect labelRect = new(toggleRect.xMax - 15f, rect.y, rect.width - toggleRect.width, rect.height);

            foldout = EditorGUI.Foldout(toggleRect, foldout, GUIContent.none, true);
            EditorGUI.LabelField(labelRect, iconLabel, EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
            return foldout;
        }
    }
}
