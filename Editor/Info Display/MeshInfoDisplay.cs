// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

#if UNITY_EDITOR
namespace Nementic.MeshDebugging
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class MeshInfoDisplay : EditorWindow
    {
        [MenuItem("Nementic/Mesh Debugging/Info Display")]
        public static void ShowWindow()
        {
            var window = GetWindow<MeshInfoDisplay>();
            window.titleContent = new GUIContent("Mesh Info", window.icon);
        }

#pragma warning disable 0649
        [SerializeField]
        private Texture2D icon;
#pragma warning restore 0649

        private MeshFilter meshFilter;
        private Mesh mesh;
        private bool findMeshInSelection = true;
        private bool showNormals = false;
        private bool showVertexIndices = false;
        private float normalsLength = 0.25f;
        private bool normalizeDisplay;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();

        private void OnEnable()
        {
            SceneView.duringSceneGui += DrawGizmos;
            OnSelectionChange();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawGizmos;
        }

        private void OnSelectionChange()
        {
            if (findMeshInSelection)
            {
                mesh = FindMeshInSelection();
                if (mesh == null)
                    meshFilter = null;
            }

            Repaint();
        }

        private Mesh FindMeshInSelection()
        {
            Mesh mesh = null;
            if (Selection.activeGameObject != null)
            {
                meshFilter = Selection.activeGameObject.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    mesh = meshFilter.sharedMesh;
                }
                else
                {
                    var skinnedMeshRenderer = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
                    if (skinnedMeshRenderer != null)
                    {
                        mesh = new Mesh();
                        skinnedMeshRenderer.BakeMesh(mesh);
                    }
                }
            }
            else if (Selection.activeObject != null && Selection.activeObject is Mesh m)
            {
                mesh = m;
            }
            return mesh;
        }

        public void OnGUI()
        {
            EditorGUILayout.Space();

            findMeshInSelection = EditorGUILayout.Toggle("Mesh From Selection", findMeshInSelection);

            if (findMeshInSelection)
                EditorGUILayout.LabelField("Target", mesh != null ? mesh.name.ToString() : "None Selected");
            else
                mesh = (Mesh)EditorGUILayout.ObjectField("Target", mesh, typeof(Mesh), allowSceneObjects: true);

            EditorGUI.BeginChangeCheck();
            showNormals = EditorGUILayout.Toggle("Show Normals", showNormals);
            normalsLength = EditorGUILayout.Slider("Normal Length", normalsLength, 0.0001f, 1f);
            normalizeDisplay = EditorGUILayout.Toggle("Force Normalize", normalizeDisplay);
            showVertexIndices = EditorGUILayout.Toggle("Show Vertex Indices", showVertexIndices);
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();

            if (mesh != null)
            {
                EditorGUILayout.LabelField("Mesh Info", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField("Vertex Count: " + mesh.vertexCount);
                EditorGUILayout.LabelField("UV1: " + mesh.uv.Length);
                EditorGUILayout.LabelField("UV2: " + mesh.uv2.Length);
                EditorGUILayout.LabelField("UV3: " + mesh.uv3.Length);
                EditorGUILayout.LabelField("UV4: " + mesh.uv4.Length);
                EditorGUILayout.LabelField("Colors: " + mesh.colors.Length);
                EditorGUILayout.LabelField("Tangents: " + mesh.tangents.Length);
                EditorGUILayout.LabelField("Bounds: " + mesh.bounds);
            }
        }

        private void DrawGizmos(SceneView sceneView)
        {
            if (mesh != null)
            {
                if (meshFilter != null)
                {
                    Handles.matrix = meshFilter.transform.localToWorldMatrix;
                }

                mesh.GetVertices(vertices);
                mesh.GetNormals(normals);

                for (int i = 0; i < mesh.vertexCount; i++)
                {
                    if (showNormals)
                    {
                        Handles.color = Color.yellow;

                        var normal = normalizeDisplay ? normals[i].normalized : normals[i];

                        Handles.DrawLine(
                            vertices[i],
                            vertices[i] + normal * normalsLength);
                    }

                    if (showVertexIndices)
                        LabelCentered(vertices[i], Vector2.zero, i.ToString());
                }
            }
        }

        public static void LabelCentered(Vector3 position, Vector2 screenSpaceOffset, string display)
        {
            Vector2 guiPos = HandleUtility.WorldToGUIPoint(position);
            guiPos += screenSpaceOffset;

            Handles.BeginGUI();

            GUIContent content = new GUIContent(display);
            Vector2 size = GUI.skin.label.CalcSize(content);

            var rect = new Rect(
                guiPos - (size / 2f),
                size);

            GUI.backgroundColor = Color.black;
            Rect boxRect = Rect.MinMaxRect(
                rect.xMin - 4,
                rect.yMin - 2,
                rect.xMax + 4,
                rect.yMax + 2);

            EditorGUI.DrawRect(
                boxRect,
                new Color(0.3f, 0.3f, 0.3f, 0.8f));

            GUI.backgroundColor = Color.white;
            GUI.Label(rect, content);

            Handles.EndGUI();
        }
    }
}
#endif
