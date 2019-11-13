// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

#if UNITY_EDITOR
namespace Nementic.MeshDebugging
{
    using UnityEditor;
    using UnityEngine;

    public class MeshSceneViewShading : EditorWindow
    {
        private SceneView sceneView;
        private int selectedShader;
        private string[] shaderNames;
        private Shader[] shaders;

        [MenuItem("Nementic/Mesh Debugging/Scene View Shading")]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow<MeshSceneViewShading>("Scene View Shading");
            window.minSize = new Vector2(50, 28);
        }

        private void OnEnable()
        {
            string[] guids = AssetDatabase.FindAssets("t:Shader", new[] { "Packages/com.nementic.mesh-debugging/Editor/SceneView Shading" });
            shaders = new Shader[guids.Length + 1];
            shaderNames = new string[shaders.Length];
            shaderNames[0] = "None";

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                int dropdownIndex = i + 1; // First entry is the 'none' value.
                shaders[dropdownIndex] = AssetDatabase.LoadAssetAtPath<Shader>(path);
                shaderNames[dropdownIndex] = shaders[dropdownIndex].name.Replace("Hidden/", "");
            }
        }

        private void OnDestroy()
        {
            ResetSceneViewMode(sceneView);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            selectedShader = EditorGUILayout.Popup("Shading Mode", selectedShader, shaderNames);
            if (EditorGUI.EndChangeCheck())
                SetShadingMode(selectedShader);
        }

        private void SetShadingMode(int mode)
        {
            if (sceneView != SceneView.lastActiveSceneView)
            {
                ResetSceneViewMode(sceneView);
                sceneView = SceneView.lastActiveSceneView;
            }

            sceneView.SetSceneViewShaderReplace(shaders[mode], null);
            sceneView.Repaint();
        }

        private static void ResetSceneViewMode(SceneView sceneView)
        {
            if (sceneView != null)
            {
                sceneView.SetSceneViewShaderReplace(null, null);
                sceneView.Repaint();
            }
        }
    }
}
#endif
