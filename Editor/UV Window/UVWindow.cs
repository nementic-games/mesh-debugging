// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

#if UNITY_EDITOR
namespace Nementic.MeshDebugging.UV
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEditor;
	using UnityEngine;
	using Object = UnityEngine.Object;

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

		public void Inspect(GameObject gameObject)
		{
			selectionManager.Refresh(new[] { gameObject });
		}

#pragma warning disable 0649
		[SerializeField]
		private Texture2D icon;
#pragma warning restore 0649

		[SerializeField]
		private SelectionManager selectionManager = new SelectionManager();

		[SerializeField]
		private OptionsPanel optionsPanel = new OptionsPanel();

		private bool scheduleFocusView;
		private Vector2 origin;
		private float zoom = 1f;
		private bool validDragStarted;
		private bool optionsPanelSizeSet;
		private Rect optionsRect;
		private GUIStyle toolbarButtonStyle;

		private const float unitPixelSize = 300f;
		private readonly List<Vector2> uvBuffer = new List<Vector2>(64);
		private readonly List<int> triangleBuffer = new List<int>(32);

		private void OnEnable()
		{
			base.wantsMouseMove = true;
			selectionManager.OnEnable(this);
			OnSelectionChange();
			EditorApplication.hierarchyChanged += OnSelectionChange;
			ObjectChangeEvents.changesPublished += OnChangesPublished;
		}

		private void OnChangesPublished(ref ObjectChangeEventStream stream)
		{
			for (int i = 0; i < stream.length; i++)
			{
				if (stream.GetEventType(i) == ObjectChangeKind.ChangeAssetObjectProperties)
				{
					stream.GetChangeAssetObjectPropertiesEvent(i,
						out ChangeAssetObjectPropertiesEventArgs data);

					Object target = EditorUtility.InstanceIDToObject(data.instanceId);
					if (target is Material)
						OnSelectionChange();
				}
			}
		}

		private void OnDisable()
		{
			ObjectChangeEvents.changesPublished -= OnChangesPublished;
			EditorApplication.hierarchyChanged -= OnSelectionChange;
		}

		private void OnDestroy()
		{
			if (selectionManager != null)
				selectionManager.OnDestroy();
		}

		private void OnSelectionChange()
		{
			selectionManager.Refresh(Selection.gameObjects);
			Repaint();
		}

		private void OnGUI()
		{
			if (scheduleFocusView)
			{
				FocusView();
				scheduleFocusView = false;
			}

			Rect toolbarRect = new Rect(-1, 0, position.width, 20);
			Toolbar(toolbarRect, selectionManager);

			Rect graphWindowRect = new Rect(0, toolbarRect.yMax + 1, position.width,
				position.height - toolbarRect.height - 2);

			if (UVWindowSettings.showOptions.value == true)
			{
				if (optionsPanelSizeSet == false)
				{
					optionsPanel.normalizedPosition = (EditorGUIUtility.currentViewWidth - 230) /
					                                  EditorGUIUtility.currentViewWidth;
					optionsPanelSizeSet = true;
				}

				optionsRect = new Rect(OptionsPanelPosition, toolbarRect.yMax,
					EditorGUIUtility.currentViewWidth - OptionsPanelPosition, graphWindowRect.height);
				graphWindowRect.xMax = optionsRect.xMin;

				optionsPanel.Draw(optionsRect, selectionManager.Meshes, uvBuffer, this);
			}

			HandlesMouseEvents(new Vector2(0f, -toolbarRect.height), unitPixelSize, graphWindowRect);
			GraphArea(graphWindowRect, unitPixelSize, selectionManager.Meshes);
		}

		private float OptionsPanelPosition
		{
			get => Mathf.Clamp(optionsPanel.normalizedPosition * EditorGUIUtility.currentViewWidth, 50,
				EditorGUIUtility.currentViewWidth - 150);
		}

		private void Toolbar(Rect toolbarRect, SelectionManager selectionManager)
		{
			GUILayout.BeginArea(toolbarRect, EditorStyles.toolbar);
			GUILayout.BeginHorizontal();

			selectionManager.DrawInfoLabel();
			GUILayout.FlexibleSpace();

			InitializeStyles();

			bool hide = selectionManager.Meshes.Any(x => x.TextureAlpha > 0.01f);
			string label = hide ? "Hide Textures" : "Show Textures";
			if (GUILayout.Button(label, toolbarButtonStyle, GUILayout.Width(100)))
				SetAllTexturesVisibility(hide);

			if (GUILayout.Button("Center View", toolbarButtonStyle, GUILayout.Width(80)))
				FocusView();

			GUILayout.Space(10f);

			EditorGUI.BeginChangeCheck();
			UVWindowSettings.searchChildren.value = GUILayout.Toggle(UVWindowSettings.searchChildren,
				"Include Children", EditorStyles.toolbarButton);
			if (EditorGUI.EndChangeCheck())
				OnSelectionChange();

			UVWindowSettings.showOptions.value = GUILayout.Toggle(UVWindowSettings.showOptions,
				"Display Options", EditorStyles.toolbarButton);

			GUILayout.Space(4);

			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}

		private void SetAllTexturesVisibility(bool hide)
		{
			foreach (var m in selectionManager.Meshes)
				m.TextureAlpha = hide ? 0f : 1f;
		}

		private void GraphArea(Rect graphWindowRect, float unitPixelSize,
			IEnumerable<SelectedMesh> meshTargets)
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

			// Draw textures first and overlay UVs on top
			// to support comparing UVs from different meshes
			// while checking against a common texture atlas.
			foreach (SelectedMesh meshTarget in meshTargets)
			{
				if (meshTarget != null)
					DrawTexture(meshTarget, origin, zoomedPixelSize);
			}

			foreach (SelectedMesh meshTarget in meshTargets)
			{
				if (meshTarget != null)
					DrawUVs(meshTarget, origin, zoomedPixelSize);
			}

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
					optionsPanel.normalizedPosition = (EditorGUIUtility.currentViewWidth - 230) /
					                                  EditorGUIUtility.currentViewWidth;
					optionsPanelSizeSet = true;
				}

				var optionsRect = new Rect(OptionsPanelPosition, 0,
					EditorGUIUtility.currentViewWidth - OptionsPanelPosition, 0);
				windowSize.x -= optionsRect.width;
			}
			Vector2 graphOrigin = windowSize * 0.5f;

			Rect bounds = new Rect();

			foreach (var target in selectionManager.Meshes)
			{
				Rect rect = CalculateUVBounds(target);
				RectUnion(ref rect, ref bounds, out bounds);
			}

			Vector2 uvMapSize = bounds.size * 0.5f * unitPixelSize;
			uvMapSize.y *= -1f;
			origin = graphOrigin - uvMapSize;
			zoom = 1f;
			Repaint();
		}

		public static void RectUnion(ref Rect RA, ref Rect RB, out Rect RUnion)
		{
			RUnion = new Rect();
			RUnion.min = Vector2.Min(RA.min, RB.min);
			RUnion.max = Vector2.Max(RA.max, RB.max);
		}

		private void DrawTexture(SelectedMesh selectedMesh, Vector2 position, float scale)
		{
			if (selectedMesh.TryGetMaterial(out Material material))
				selectedMesh.DrawPreviewTexture(material, position, scale);
		}

		private void DrawUVs(SelectedMesh selectedMesh, Vector2 position, float scale)
		{
			Mesh mesh = selectedMesh.Mesh;

			if (mesh == null)
				return;

			if (mesh.vertexCount == 0)
				return;

			if (selectedMesh.uvChannel == UVChannel.None)
				return;

			mesh.GetUVs((int)selectedMesh.uvChannel, uvBuffer);

			if (uvBuffer.Count == 0)
				return;

			mesh.GetTriangles(triangleBuffer, 0);

			if (triangleBuffer.Count == 0)
				return;

			position.y = -position.y;

			GraphBackground.ApplyWireMaterial();
			GL.PushMatrix();
			GL.Begin(GL.LINES);

			GL.Color(selectedMesh.UVColorWithAlpha);

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

		private Rect CalculateUVBounds(SelectedMesh selectedMesh)
		{
			Rect bounds = new Rect();

			if (uvBuffer.Count == 0 && selectedMesh.Mesh != null)
				selectedMesh.Mesh.GetUVs((int)selectedMesh.uvChannel, uvBuffer);

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
