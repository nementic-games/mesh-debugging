// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

#if UNITY_EDITOR
namespace Nementic.MeshDebugging
{
	using System.Collections.Generic;
	using System.Linq;
	using UnityEditor;
	using UnityEditor.SettingsManagement;
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

		public class WindowSetting<T> : UserSetting<T>
		{
			public WindowSetting(string key, T value, SettingsScope scope = SettingsScope.User)
				: base(UVWindow.settings, key, value, scope)
			{
			}
		}

		private static readonly Settings settings = new Settings("com.nementic.mesh-debugging");

		public Mesh Mesh
		{
			set => meshSource.Mesh = value;
		}

		private MeshSource meshSource;

		[UserSetting("UV", "Alpha")]
		private static UserSetting<float> uvMeshAlpha = new WindowSetting<float>("uvMeshAlpha", 1f);

		[UserSetting("UV", "Color")]
		private static UserSetting<Color> uvColor = new WindowSetting<Color>("uvColor", Color.cyan * 0.95f);

		[UserSetting("Texture", "Alpha")]
		private static UserSetting<float> textureAlpha = new WindowSetting<float>("textureAlpha", 1f);

		[SettingsProvider]
		private static SettingsProvider CreateSettingsProvider()
		{
			return new UserSettingsProvider("Nementic/UV Window",
				settings,
				new[] { typeof(UVWindow).Assembly });
		}

		private Vector2 origin;
		private float zoom = 1f;
		private readonly float unitPixelSize = 300f;
		private UVChannel uvChannel = UVChannel.UV0;
		private string[] colorChannelLabels = new string[] { "RGB", "R", "G", "B" };
		private ColorChannel colorChannel = ColorChannel.All;
		private string texturePropertyName = "_MainTex";
		private string[] texturePropertyNames = new string[] { "_MainTex", "_BumpMap" };

		private Material previewMaterial;
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

		private enum ColorChannel
		{
			All,
			R,
			G,
			B
		}

		private void OnEnable()
		{
			base.wantsMouseMove = true;

			if (meshSource == null)
				meshSource = new MeshSource(this);

			// Selection is null during OnEnable in play mode.
			// Maybe this is Unity bug, but a frame later its valid again.
			EditorApplication.delayCall += () =>
			{
				meshSource.Refresh();
				Repaint();
			};

			UpdatePreviewMaterialColorChannel();
		}

		private void OnDisable()
		{
			resize = false;
		}

		private void Awake()
		{
			if (previewMaterial == null)
			{
				Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.nementic.mesh-debugging/Editor/UV Window/UV-Preview.shader");
				previewMaterial = new Material(shader);
				previewMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
		}

		private void OnDestroy()
		{
			if (previewMaterial != null)
			{
				Object.DestroyImmediate(previewMaterial);
				previewMaterial = null;
			}
		}

		private void OnSelectionChange()
		{
			meshSource.Refresh();
			Repaint();
		}

		private void OnLostFocus()
		{
			resize = false;
		}

		private bool resize;
		private bool optionsPanelSizeSet;
		private float optionsPanelNormalisedPosition;

		private void OnGUI()
		{
			Rect toolbarRect = new Rect(0, -1, position.width, EditorGUIUtility.singleLineHeight);
			Toolbar(toolbarRect, meshSource);

			Rect graphWindowRect = new Rect(0, toolbarRect.yMax + 2, position.width, position.height - toolbarRect.height - 2);

			if (showOptions)
			{
				if (optionsPanelSizeSet == false)
				{
					optionsPanelNormalisedPosition = (EditorGUIUtility.currentViewWidth - 230) / EditorGUIUtility.currentViewWidth;
					optionsPanelSizeSet = true;
				}

				float optionsPanelPosition = Mathf.Clamp(optionsPanelNormalisedPosition * EditorGUIUtility.currentViewWidth, 50, EditorGUIUtility.currentViewWidth - 150);
				var optionsRect = new Rect(optionsPanelPosition, graphWindowRect.yMin, EditorGUIUtility.currentViewWidth - optionsPanelPosition, graphWindowRect.height);
				graphWindowRect.xMax = optionsRect.xMin;

				DrawOptionsPanel(optionsRect);
			}

			HandlesMouseEvents(new Vector2(0f, -toolbarRect.height), unitPixelSize, graphWindowRect);
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
			Rect cursorRect = rect;
			cursorRect.width = 10;
			cursorRect.x -= 5;

			if (Event.current.type == EventType.MouseDown && cursorRect.Contains(Event.current.mousePosition))
			{
				resize = true;
				Event.current.Use();
			}

			if (resize && Event.current.type == EventType.MouseDrag)
			{
				optionsPanelNormalisedPosition = Event.current.mousePosition.x / EditorGUIUtility.currentViewWidth;
				Event.current.Use();

				if (Event.current.type == EventType.MouseDrag)
					Repaint();
			}

			if (Event.current.type == EventType.MouseUp)
				resize = false;

			EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.ResizeHorizontal);

			rect = rect.Expand(-4);
			GUILayout.BeginArea(rect);

			float labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 100;

			EditorGUILayout.LabelField("UV", EditorStyles.boldLabel);
			EditorGUI.indentLevel++;

			uvMeshAlpha.value = EditorGUILayout.Slider("Alpha", uvMeshAlpha, 0f, 1f);
			uvColor.value = EditorGUILayout.ColorField(
				new GUIContent("Color"),
				uvColor,
				showEyedropper: true,
				showAlpha: false,
				hdr: false);

			EditorGUI.BeginChangeCheck();
			uvChannel = (UVChannel)EditorGUILayout.EnumPopup("Channel", uvChannel);
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

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Texture", EditorStyles.boldLabel);
			EditorGUI.indentLevel++;

			EditorGUI.BeginChangeCheck();
			textureAlpha.value = EditorGUILayout.Slider("Alpha", textureAlpha, 0f, 1f);
			if (EditorGUI.EndChangeCheck())
				previewMaterial.SetFloat("_Alpha", textureAlpha);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Color");

			EditorGUI.BeginChangeCheck();
			colorChannel = (ColorChannel)GUILayout.Toolbar((int)colorChannel, colorChannelLabels, GUI.skin.button, GUI.ToolbarButtonSize.FitToContents);
			if (EditorGUI.EndChangeCheck())
				UpdatePreviewMaterialColorChannel();

			EditorGUILayout.EndHorizontal();

			using (new EditorGUI.DisabledScope(this.meshSource.HasMaterial == false))
			{
				if (this.meshSource.HasMaterial)
				{
					string[] names = this.meshSource.Material.GetTexturePropertyNames()
						.Where(x => this.meshSource.Material.HasProperty(x)).ToArray();

					int selectedIndex = System.Array.IndexOf(names, texturePropertyName);

					if (selectedIndex == -1)
						selectedIndex = 0;

					EditorGUI.BeginChangeCheck();
					selectedIndex = EditorGUILayout.Popup("Source Map", selectedIndex, names);
					if (EditorGUI.EndChangeCheck())
						texturePropertyName = names[selectedIndex];
				}
				else
					EditorGUILayout.LabelField("Source Map", "<None>");
			}

			EditorGUI.indentLevel--;

			EditorGUIUtility.labelWidth = labelWidth;
			GUILayout.EndArea();

			cursorRect.xMax -= 3f;
			cursorRect.yMin += 1;
			EditorGUI.DrawRect(cursorRect, new Color(0.15f, 0.15f, 0.15f));
		}

		private void UpdatePreviewMaterialColorChannel()
		{
			Vector4 colorMask = Vector4.one;
			switch (colorChannel)
			{
				case ColorChannel.R:
					colorMask = new Vector4(1, 0, 0, 1);
					break;
				case ColorChannel.G:
					colorMask = new Vector4(0, 01, 0, 1);
					break;
				case ColorChannel.B:
					colorMask = new Vector4(0, 0, 1, 1);
					break;
			}
			previewMaterial.SetVector("_ColorMask", colorMask);
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

		private bool validDragStarted = false;

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
			if (this.meshSource.HasMaterial)
			{
				Texture texture = this.meshSource.Material.GetTexture(texturePropertyName);

				if (texture != null)
				{
					Vector2 textureOffset = this.meshSource.Material.GetTextureOffset(texturePropertyName);
					Vector2 textureScale = this.meshSource.Material.GetTextureScale(texturePropertyName);
					Vector2 texturePosition = position;

					texturePosition -= new Vector2(textureOffset.x, textureOffset.y) * scale / new Vector2(textureScale.x, -textureScale.y);
					texturePosition.y -= 1f / textureScale.y * scale;

					textureScale = new Vector2(scale, scale) / textureScale;

					Rect rect = new Rect(texturePosition, textureScale);

					int isBumpMap = texturePropertyName.Contains("Bump") ? 1 : 0;
					previewMaterial.SetInt("_IsBumpMap", isBumpMap);
					Graphics.DrawTexture(rect, texture, previewMaterial);
				}
			}

			position.y = -position.y;

			GraphBackground.ApplyWireMaterial();
			GL.PushMatrix();
			GL.Begin(GL.LINES);

			var color = uvColor.value;
			color.a = uvMeshAlpha;
			GL.Color(color);

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
