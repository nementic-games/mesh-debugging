﻿// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

#if UNITY_EDITOR
namespace Nementic.MeshDebugging.UV
{
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;

	public class UVWindow : EditorWindow
	{
		[MenuItem("Nementic/Mesh Debugging/UV Window")]
		public static UVWindow GetWindow()
		{
			var window = GetWindow<UVWindow>();
			window.titleContent = new GUIContent("UV Window", window.icon);

			// Start the graph origin at the window center as a fallback. 
			window.origin = window.position.size * 0.5f;

			// Once initialization is over, focus the UV map if possible.
			window.scheduleFocusView = true;

			return window;
		}

#pragma warning disable 0649
		[SerializeField]
		private Texture2D icon;
#pragma warning restore 0649

		public Mesh Mesh
		{
			get { return meshSource.Mesh; }
			set { meshSource.Mesh = value; }
		}

		private MeshSource meshSource;
		private bool needsMeshRefresh;
		private readonly List<Vector2> uvBuffer = new List<Vector2>(64);
		private readonly List<int> triangleBuffer = new List<int>(32);

		private bool scheduleFocusView;
		private Vector2 origin;
		private float zoom = 1f;
		private bool validDragStarted = false;
		private static readonly float unitPixelSize = 300f;

		private Options options = new Options();
		private bool optionsPanelSizeSet;
		private Rect optionsRect;

		private GUIStyle toolbarButtonStyle;

		private void OnEnable()
		{
			if (meshSource == null)
				meshSource = new MeshSource(this);

			base.wantsMouseMove = true;
			options.OnEnable();
			needsMeshRefresh = true;
		}

		private void OnDisable()
		{
			options.OnDisable();
		}

		private void OnSelectionChange()
		{
			meshSource.Refresh();
			Repaint();
		}

		private void OnGUI()
		{
			if (needsMeshRefresh)
			{
				meshSource.Refresh();
				needsMeshRefresh = false;
			}

			if (scheduleFocusView)
			{
				FocusView();
				scheduleFocusView = false;
			}

			Rect toolbarRect = new Rect(-1, 0, position.width, 20);
			Toolbar(toolbarRect, meshSource);

			Rect graphWindowRect = new Rect(0, toolbarRect.yMax + 1, position.width, position.height - toolbarRect.height - 2);

			if (UVWindowSettings.showOptions.value == true)
			{
				if (optionsPanelSizeSet == false)
				{
					options.normalizedPosition = (EditorGUIUtility.currentViewWidth - 230) / EditorGUIUtility.currentViewWidth;
					optionsPanelSizeSet = true;
				}

				optionsRect = new Rect(OptionsPanelPosition, toolbarRect.yMax, EditorGUIUtility.currentViewWidth - OptionsPanelPosition, graphWindowRect.height);
				graphWindowRect.xMax = optionsRect.xMin;

				options.Draw(optionsRect, meshSource, uvBuffer, this);
			}

			HandlesMouseEvents(new Vector2(0f, -toolbarRect.height), unitPixelSize, graphWindowRect);
			GraphArea(graphWindowRect, unitPixelSize, meshSource);
		}

		private float OptionsPanelPosition
		{
			get => Mathf.Clamp(options.normalizedPosition * EditorGUIUtility.currentViewWidth, 50, EditorGUIUtility.currentViewWidth - 150);
		}

		private void Toolbar(Rect toolbarRect, MeshSource meshSource)
		{
			GUILayout.BeginArea(toolbarRect, EditorStyles.toolbar);
			GUILayout.BeginHorizontal();

			meshSource.DrawOptionPicker();
			GUILayout.FlexibleSpace();

			InitializeStyles();

			if (GUILayout.Button("Focus", toolbarButtonStyle, GUILayout.Width(50)))
				FocusView();

			UVWindowSettings.showOptions.value = GUILayout.Toggle(UVWindowSettings.showOptions, "Options", EditorStyles.toolbarButton);

			GUILayout.Space(4);

			GUILayout.EndHorizontal();
			GUILayout.EndArea();
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

		private void HandlesMouseEvents(Vector2 mouseCorrection, float unitPixelSize, Rect graphWindowRect)
		{
			Event current = Event.current;

			if (current.type == EventType.MouseDown && graphWindowRect.Contains(current.mousePosition))
				validDragStarted = true;

			if (current.type == EventType.MouseUp)
				validDragStarted = false;

			if (validDragStarted && current.type == EventType.MouseDrag && current.button != -1)
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
				FocusView();
				current.Use();
			}
		}

		private void ZoomView(Vector2 mousePosition, float zoomDelta, float unitPixelSize)
		{
			float previousZoom = zoom;
			zoom = UVWindowSettings.PerformZoom(zoom, zoomDelta);

			Vector2 mouseOffset = (mousePosition - origin);
			Vector2 gridCoord = mouseOffset / (unitPixelSize * previousZoom);
			Vector2 offset = gridCoord * unitPixelSize * zoom - mouseOffset;

			origin -= offset;
			Repaint();
		}

		private void FocusView()
		{
			Vector2 windowSize = base.position.size;
			if (UVWindowSettings.showOptions)
			{
				if (optionsPanelSizeSet == false)
				{
					options.normalizedPosition = (EditorGUIUtility.currentViewWidth - 230) / EditorGUIUtility.currentViewWidth;
					optionsPanelSizeSet = true;
				}

				var optionsRect = new Rect(OptionsPanelPosition, 0, EditorGUIUtility.currentViewWidth - OptionsPanelPosition, 0);
				windowSize.x -= optionsRect.width;
			}
			Vector2 graphOrigin = windowSize * 0.5f;

			Vector2 uvMapSize = CalculateUVBounds().size * 0.5f * unitPixelSize;
			uvMapSize.y *= -1f;
			origin = graphOrigin - uvMapSize;
			zoom = 1f;
			Repaint();
		}

		private void DrawUVs(Mesh mesh, Vector2 position, float scale)
		{
			if (mesh == null)
				return;

			if (mesh.vertexCount == 0)
				return;

			mesh.GetUVs((int)options.uvChannel, uvBuffer);

			if (uvBuffer.Count == 0)
				return;

			mesh.GetTriangles(triangleBuffer, 0);

			if (triangleBuffer.Count == 0)
				return;

			// TODO: Ensure preview is drawn behind labels.
			if (this.meshSource.HasMaterial)
			{
				options.DrawPreviewTexture(this.meshSource.Material, position, scale);
			}

			position.y = -position.y;

			GraphBackground.ApplyWireMaterial();
			GL.PushMatrix();
			GL.Begin(GL.LINES);

			GL.Color(UVWindowSettings.UVColorWithAlpha);

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

		private Rect CalculateUVBounds()
		{
			Rect bounds = new Rect();

			if (uvBuffer.Count == 0 && this.Mesh != null)
				this.Mesh.GetUVs((int)options.uvChannel, uvBuffer);

			if (uvBuffer.Count > 0)
			{
				bounds.center = uvBuffer[0];
				for (int i = 1; i < uvBuffer.Count; i++)
				{
					Vector2 uv = uvBuffer[i];

					if (uv.x < bounds.xMin)
						bounds.xMin = uv.x;

					else if (uv.x > bounds.xMax)
						bounds.xMax = uv.x;

					if (uv.y < bounds.yMin)
						bounds.yMin = uv.y;

					else if (uv.y > bounds.yMax)
						bounds.yMax = uv.y;
				}
			}

			return bounds;
		}
	}
}
#endif
