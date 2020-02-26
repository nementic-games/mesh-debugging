// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

#if UNITY_EDITOR
namespace Nementic.MeshDebugging
{
	using System;
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;

	public class UVWindow : EditorWindow
	{
		[MenuItem("Nementic/Mesh Debugging/UV Window")]
		public static UVWindow GetWindow()
		{
			var window = GetWindow<UVWindow>("UV Window");
			window.origin = window.position.size * 0.5f;
			return window;
		}

		public Mesh Mesh
		{
			set => meshSource.Mesh = value;
		}

		private MeshSource meshSource;

		private Vector2 origin;
		private float zoom = 1f;
		private readonly float unitPixelSize = 300f;
		private UVChannel uvChannel = UVChannel.UV0;
		private int materialChannel = 0;
		private bool showOptions;

		private GUIStyle toolbarButtonStyle;
		private static readonly float zoomSpeed = 0.1f;
		private static readonly float minZoom = 0.5f;
		private static readonly float maxZoom = 10f;
		private readonly List<Vector2> uvBuffer = new List<Vector2>(64);
		private readonly List<int> triangleBuffer = new List<int>(32);

		private enum UVChannel
		{
			UV0,
			UV1,
			UV2,
			UV3,
			UV4,
			UV5,
			UV6,
			UV7,
		}

		private void OnEnable()
		{
			base.wantsMouseMove = true;

			if (meshSource == null)
				meshSource = new MeshSource(this);
		}

		private void OnSelectionChange()
		{
			meshSource.Refresh();
			Repaint();
		}

		private void OnGUI()
		{
			Rect toolbarRect = new Rect(0, -1, position.width, EditorGUIUtility.singleLineHeight);
			Toolbar(toolbarRect, meshSource);

			HandlesMouseEvents(new Vector2(0f, -toolbarRect.height), unitPixelSize);

			Rect graphWindowRect = new Rect(0, toolbarRect.yMax + 2, position.width, position.height - toolbarRect.height - 2);

			if (showOptions)
			{
				const float optionsWidth = 200;
				graphWindowRect.xMax -= optionsWidth;

				Rect optionsRect = new Rect(graphWindowRect.xMax, graphWindowRect.yMin, optionsWidth, graphWindowRect.height);
				DrawOptionsPanel(optionsRect);
			}

			GraphArea(graphWindowRect, unitPixelSize, meshSource);
		}

		private void Toolbar(Rect toolbarRect, MeshSource meshSource)
		{
			GUILayout.BeginArea(toolbarRect, EditorStyles.toolbar);
			GUILayout.BeginHorizontal();

			meshSource.DrawOptionPicker();
			GUILayout.FlexibleSpace();

			InitializeStyles();

			if (GUILayout.Button("Recenter", toolbarButtonStyle, GUILayout.Width(70)))
				ResetView();

			showOptions = GUILayout.Toggle(showOptions, "Options", EditorStyles.toolbarButton);

			GUILayout.Space(4);

			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}

		private void DrawOptionsPanel(Rect rect)
		{
			rect = rect.Expand(-4);
			GUILayout.BeginArea(rect);

			float labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 100;

			materialChannel = EditorGUILayout.Toggle("Show Texture", materialChannel == 0) ? 0 : -1;

			EditorGUI.BeginChangeCheck();
			uvChannel = (UVChannel)EditorGUILayout.EnumPopup("UV Channel", uvChannel);
			if (EditorGUI.EndChangeCheck())
			{
				if (this.meshSource != null)
				{
					this.meshSource.Mesh.GetUVs((int)uvChannel, uvBuffer);

					if (uvBuffer.Count == 0)
						base.ShowNotification(new GUIContent($"No {uvChannel.ToString()} found."));
					else
						base.RemoveNotification();
				}
			}

			EditorGUIUtility.labelWidth = labelWidth;
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
			// UV maps usually cover the range [0..1], so reset to the center
			// of the first quadrant instead of the graph origin.
			Vector2 graphOrigin = base.position.size * 0.5f;
			Vector2 quadrantSize = new Vector2(0.5f, -0.5f) * unitPixelSize;
			origin = graphOrigin - quadrantSize;
			zoom = 1f;
			Repaint();
		}

		private void DrawUVs(Mesh mesh, Vector2 position, float scale)
		{
			if (mesh == null)
				return;

			if (mesh.vertexCount == 0)
				return;

			mesh.GetUVs((int)uvChannel, uvBuffer);

			if (uvBuffer.Count == 0)
				return;

			mesh.GetTriangles(triangleBuffer, 0);

			if (triangleBuffer.Count == 0)
				return;

			// TODO: Ensure preview is drawn behind labels.
			if (this.materialChannel == 0 && this.meshSource.HasPreviewMaterial && this.meshSource.PreviewMaterial.mainTexture != null)
			{
				Vector2 textureOffset = this.meshSource.PreviewMaterial.mainTextureOffset;
				Vector2 textureScale = this.meshSource.PreviewMaterial.mainTextureScale;
				Vector2 texturePosition = position;

				texturePosition -= new Vector2(textureOffset.x, textureOffset.y) * scale / new Vector2(textureScale.x, -textureScale.y);
				texturePosition.y -= 1f / textureScale.y * scale;

				textureScale = new Vector2(scale, scale) / textureScale;

				Rect rect = new Rect(texturePosition, textureScale);
				EditorGUI.DrawPreviewTexture(rect, this.meshSource.PreviewMaterial.mainTexture);
			}

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
