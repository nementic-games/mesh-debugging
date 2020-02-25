// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

namespace Nementic.MeshDebugging
{
    using UnityEditor;
    using UnityEngine;

    [System.Serializable]
    public class MeshSource
    {
        public Mesh Mesh
        {
            get => mesh;
            set
            {
                mesh = value;
                hostWindow.Repaint();
            }
        }

        public Material PreviewMaterial => previewMaterial;

        public bool HasPreviewMaterial => previewMaterial != null;

        private Mesh mesh;
        private MeshSourceMode mode;
        private Material previewMaterial;
        private GameObject gameObject;
        private EditorWindow hostWindow;

        public MeshSource(EditorWindow host)
        {
            hostWindow = host;
        }

        public void DrawOptionPicker()
        {
            EditorGUI.BeginChangeCheck();
            mode = (MeshSourceMode)EditorGUILayout.EnumPopup(mode, EditorStyles.toolbarDropDown, GUILayout.Width(100));
            if (EditorGUI.EndChangeCheck())
                Refresh();

            switch (mode)
            {
                case MeshSourceMode.MeshAsset:
                    this.mesh = EditorGUILayout.ObjectField(this.mesh, typeof(Mesh), allowSceneObjects: false) as Mesh;
                    break;

                case MeshSourceMode.GameObject:
                    EditorGUI.BeginChangeCheck();
                    this.gameObject = EditorGUILayout.ObjectField(this.gameObject, typeof(GameObject), allowSceneObjects: true) as GameObject;
                    if (EditorGUI.EndChangeCheck())
                        Refresh();
                    break;

                case MeshSourceMode.Selection:
                    string label = mesh != null ? mesh.name : "<Nothing Selected>";
                    GUILayout.Label(label, EditorStyles.boldLabel);
                    break;
            }
        }

        public void Refresh()
        {
            this.mesh = null;
            this.previewMaterial = null;
            this.gameObject = null;

            switch (mode)
            {
                case MeshSourceMode.GameObject:
                    TryGetMesh(this.gameObject);
                    break;

                case MeshSourceMode.Selection:
                    TryGetMesh(Selection.activeObject);
                    break;
            }
        }

        private void TryGetMesh(Object target)
        {
            if (target == null)
                this.mesh = null;

            if (target is Mesh mesh)
                this.mesh = mesh;

            if (target is GameObject gameObject)
            {
                var meshFilter = gameObject.GetComponentInChildren<MeshFilter>();

                // TODO: Handle conflicting components by showing a dropdown
                // or a list of bubble buttons at the top?

                if (meshFilter != null)
                {
                    var renderer = meshFilter.GetComponent<Renderer>();
                    if (renderer != null)
                        previewMaterial = renderer.sharedMaterial;

                    this.mesh = meshFilter.sharedMesh;
                }
                else
                {
                    var skinnedMeshRenderer = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (skinnedMeshRenderer != null)
                    {
                        previewMaterial = skinnedMeshRenderer.sharedMaterial;

                        this.mesh = skinnedMeshRenderer.sharedMesh;
                    }
                }
            }
        }

        public static implicit operator Mesh(MeshSource meshSource) => meshSource.mesh;
    }

    public enum MeshSourceMode
    {
        Selection,
        MeshAsset,
        GameObject
    }
}
