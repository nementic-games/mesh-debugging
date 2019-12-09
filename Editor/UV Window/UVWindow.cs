﻿// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

#if UNITY_EDITOR
namespace Nementic.MeshDebugging
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class UVWindow : EditorWindow
    {
        [MenuItem("Nementic/Mesh Debugging/UV Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<UVWindow>("UV Window");
            window.origin = window.position.size * 0.5f;
        }

        [SerializeField]
        private Vector2 origin;

        [SerializeField]
        private float zoom = 1f;

        private GUIStyle toolbarButtonStyle;
        private static readonly float zoomSpeed = 0.1f;
        private static readonly float minZoom = 0.5f;
        private static readonly float maxZoom = 10f;
        private readonly List<Vector2> uvBuffer = new List<Vector2>(64);
        private readonly List<int> triangleBuffer = new List<int>(32);

        private void OnEnable()
        {
            base.wantsMouseMove = true;
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private void OnGUI()
        {
            Mesh mesh = FindMesh();

            Rect toolbarRect = new Rect(0, -1, position.width, 16);
            Toolbar(toolbarRect, mesh != null ? mesh.name : string.Empty);

            float unitPixelSize = 300f;
            HandlesMouseEvents(new Vector2(0f, -toolbarRect.height), unitPixelSize);

            Rect graphWindowRect = new Rect(0, toolbarRect.yMax + 2, position.width, position.height - toolbarRect.height - 2);
            GraphArea(graphWindowRect, unitPixelSize, mesh);
        }

        private void Toolbar(Rect toolbarRect, string label)
        {
            GUI.Box(toolbarRect, GUIContent.none, EditorStyles.toolbar);

            GUI.Label(toolbarRect, label, EditorStyles.centeredGreyMiniLabel);

            Rect buttonRect = toolbarRect;
            buttonRect.xMin = buttonRect.xMax - 65f;
            buttonRect.xMax -= 8;

            InitializeStyles();

            if (GUI.Button(buttonRect, "Recenter", toolbarButtonStyle))
                ResetView();
        }

        private void GraphArea(Rect graphWindowRect, float unitPixelSize, Mesh mesh)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            float zoomedPixelSize = unitPixelSize * zoom;
            Vector2 graphSize = Vector2.one * zoomedPixelSize * 6f;
            Rect graphRect = new Rect(-graphSize * 0.5f, graphSize);

            GUI.BeginClip(graphWindowRect);
            graphRect.position += origin;

            Rect backgroundRect = new Rect(0, 0, graphWindowRect.width, graphWindowRect.height);
            GraphBackground.DrawGraphBackground(backgroundRect, graphRect, zoomedPixelSize);

            Handles.color = Color.gray;
            Handles.DotHandleCap(0, origin, Quaternion.identity, 2.5f, EventType.Repaint);

            DrawUVs(mesh, origin, zoomedPixelSize);

            GUI.EndClip();
        }

        private void InitializeStyles()
        {
            if (toolbarButtonStyle == null)
            {
                toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(4, 0, 0, 0)
                };
            }
        }

        private void HandlesMouseEvents(Vector2 mouseCorrection, float unitPixelSize)
        {
            Event current = Event.current;

            if (current.type == EventType.MouseDrag && current.button != -1)
            {
                origin += current.delta;
                current.Use();
                Repaint();
            }
            else if (current.type == EventType.ScrollWheel)
            {
                ZoomView(current.mousePosition + mouseCorrection, current.delta.y, unitPixelSize);
                current.Use();
            }
            else if (current.isKey && current.keyCode == KeyCode.F)
            {
                ResetView();
                current.Use();
            }
        }

        private void ZoomView(Vector2 mousePosition, float zoomDelta, float unitPixelSize)
        {
            float previousZoom = zoom;
            zoom -= zoomDelta * zoomSpeed;
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);

            Vector2 mouseOffset = (mousePosition - origin);
            Vector2 gridCoord = mouseOffset / (unitPixelSize * previousZoom);
            Vector2 offset = gridCoord * unitPixelSize * zoom - mouseOffset;

            origin -= offset;
            Repaint();
        }

        private void ResetView()
        {
            origin = base.position.size * 0.5f;
            zoom = 1f;
            Repaint();
        }

        private Mesh FindMesh()
        {
            if (Selection.activeObject is Mesh mesh)
                return mesh;

            GameObject gameObject = Selection.activeGameObject;

            if (gameObject != null)
            {
                var meshFilter = gameObject.GetComponentInChildren<MeshFilter>();

                if (meshFilter == null)
                    return null;

                return meshFilter.sharedMesh;
            }

            return null;
        }

        private void DrawUVs(Mesh mesh, Vector2 position, float scale)
        {
            if (mesh == null)
                return;

            if (mesh.vertexCount == 0)
                return;

            mesh.GetUVs(0, uvBuffer);

            if (uvBuffer.Count == 0)
                return;

            mesh.GetTriangles(triangleBuffer, 0);

            if (triangleBuffer.Count == 0)
                return;

            position.y = -position.y;

            GraphBackground.ApplyWireMaterial();
            GL.PushMatrix();
            GL.Begin(GL.LINES);
            GL.Color(Color.cyan * 0.95f);

            for (int i = 0; i < triangleBuffer.Count; i += 3)
            {
                Vector3 a = position + scale * uvBuffer[triangleBuffer[i]];
                Vector3 b = position + scale * uvBuffer[triangleBuffer[i + 1]];
                Vector3 c = position + scale * uvBuffer[triangleBuffer[i + 2]];

                a.y = -a.y;
                b.y = -b.y;
                c.y = -c.y;

                GL.Vertex(a);
                GL.Vertex(b);
                GL.Vertex(b);
                GL.Vertex(c);
                GL.Vertex(c);
                GL.Vertex(a);
            }

            GL.End();
            GL.PopMatrix();
        }
    }
}
#endif