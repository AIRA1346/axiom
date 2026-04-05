using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Ultrabolt.SkyEngine
{
    [CustomEditor(typeof(SkyCore))]
    public class SkyCoreEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUIContent contentGUI = new();
            contentGUI = EditorGUIUtility.IconContent("d_Skybox Icon");
            contentGUI.text = " Sky Core";
            EditorMethods.DrawBoxGroup(contentGUI, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("statsText"));

                contentGUI = EditorGUIUtility.IconContent("ParticleSystemForceField Gizmo");
                contentGUI.text = " Stars";
                EditorMethods.DrawBoxGroup(contentGUI, () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("starDome"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("starSpeed"));
                });

                contentGUI = EditorGUIUtility.IconContent("Main Light Gizmo");
                contentGUI.text = " Sun & Moon";
                EditorMethods.DrawBoxGroup(contentGUI, () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("celestialRotation"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("moon"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sun"));
                });

                contentGUI = EditorGUIUtility.IconContent("CloudConnect@2x");
                contentGUI.text = " Weather";
                EditorMethods.DrawBoxGroup(contentGUI, () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("weather"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lowClouds"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("highClouds"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rainFx"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("weatherSpeed"));
                });

                contentGUI = EditorGUIUtility.IconContent("d_LightingSettings Icon");
                contentGUI.text = " Lights & Colors";
                EditorMethods.DrawBoxGroup(contentGUI, () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lightFadeSpeed"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sunLight"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("moonLight"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("skyTop"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("skyBottom"));
                });

                contentGUI = EditorGUIUtility.IconContent("BuildSettings.Web");
                contentGUI.text = " Fog";
                EditorMethods.DrawBoxGroup(contentGUI, () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("enableFog"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dayFogDensity"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("nightFogDensity"));
                });

                contentGUI = EditorGUIUtility.IconContent("d_UnityEditor.ProfilerWindow@2x");
                contentGUI.text = " Time";
                EditorMethods.DrawBoxGroup(contentGUI, () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("timeSpeed"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dayLength"));
                });

                contentGUI = EditorGUIUtility.IconContent("console.infoicon@2x");
                contentGUI.text = " Info";
                EditorMethods.DrawBoxGroup(contentGUI, () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dayState"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dayCount"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("timeOfDay"));
                });
            });

            serializedObject.ApplyModifiedProperties();
        }
    }
}
