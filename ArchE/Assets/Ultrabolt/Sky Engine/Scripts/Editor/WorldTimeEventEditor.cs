using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Ultrabolt.SkyEngine
{
    [CustomEditor(typeof(WorldTimeEvent))]
    public class WorldTimeEventEditor : Editor
    {
        private SerializedProperty gameEvents;
        private bool[] foldouts;

        private void OnEnable()
        {
            gameEvents = serializedObject.FindProperty("gameEvents");
            foldouts = new bool[gameEvents.arraySize];
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var targetScript = (WorldTimeEvent)target;

            GUIContent contentGUI = new();
            contentGUI = EditorGUIUtility.IconContent("d_EventSystem Icon");
            contentGUI.text = " Time Events";
            EditorMethods.DrawBoxGroup(contentGUI, () =>
            {
                for (int i = 0; i < gameEvents.arraySize; i++)
                {
                    var gameEvent = gameEvents.GetArrayElementAtIndex(i);

                    var nameProp = gameEvent.FindPropertyRelative("eventName");
                    var dayProp = gameEvent.FindPropertyRelative("day");
                    var timeProp = gameEvent.FindPropertyRelative("time");
                    var actionsProp = gameEvent.FindPropertyRelative("actions");

                    EditorMethods.DrawFoldoutGroup(new GUIContent(nameProp.stringValue), ref foldouts[i], () =>
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(nameProp);
                        if (GUILayout.Button("A", GUILayout.Width(20)))
                        {
                            targetScript.gameEvents[i].AutoName();
                            nameProp.stringValue = targetScript.gameEvents[i].eventName;
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.PropertyField(dayProp);
                        EditorGUILayout.PropertyField(timeProp);
                        EditorGUILayout.PropertyField(actionsProp);

                    }, () =>
                    {
                        foldouts[i] = false;
                        gameEvents.DeleteArrayElementAtIndex(i);
                        foldouts = new bool[gameEvents.arraySize];
                    }, "-", Color.red);
                }
            }, () =>
            {
                gameEvents.InsertArrayElementAtIndex(gameEvents.arraySize);
                foldouts = new bool[gameEvents.arraySize];
            }, "+", Color.green);

            serializedObject.ApplyModifiedProperties();
        }
    }
}