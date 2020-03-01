// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

namespace Nementic.MeshDebugging.UV
{
	using System.Collections.Generic;
	using System.Linq;
	using UnityEditor;
	using UnityEngine;

	[System.Serializable]
	public class Options
	{
		public float normalizedPosition;
		public UVChannel uvChannel = UVChannel.UV0;
		private string[] colorChannelLabels = new string[] { "RGB", "R", "G", "B" };
		private ColorChannel colorChannel = ColorChannel.All;
		private string texturePropertyName = "_MainTex";
		private string[] texturePropertyNames = new string[] { "_MainTex", "_BumpMap" };
		private bool resize;

		private Material previewMaterial;

		public void OnEnable()
		{
			if (previewMaterial == null)
			{
				Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.nementic.mesh-debugging/Editor/UV Window/UV-Preview.shader");
				previewMaterial = new Material(shader);
				previewMaterial.hideFlags = HideFlags.HideAndDontSave;
			}

			var colorMask = GetPreviewMaterialColorMask();
			previewMaterial.SetVector("_ColorMask", colorMask);
		}

		public void OnDisable()
		{
			if (previewMaterial != null)
			{
				Object.DestroyImmediate(previewMaterial);
				previewMaterial = null;
			}
		}

		public void Draw(Rect rect, MeshSource meshSource, List<Vector2> uvBuffer, EditorWindow host)
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
				normalizedPosition = Event.current.mousePosition.x / EditorGUIUtility.currentViewWidth;
				Event.current.Use();

				if (Event.current.type == EventType.MouseDrag)
					host.Repaint();
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

			UVWindowSettings.uvAlpha.value = EditorGUILayout.Slider("Alpha", UVWindowSettings.uvAlpha, 0f, 1f);
			UVWindowSettings.uvColor.value = EditorGUILayout.ColorField(
				new GUIContent("Color"),
				UVWindowSettings.uvColor,
				showEyedropper: true,
				showAlpha: false,
				hdr: false);

			EditorGUI.BeginChangeCheck();
			uvChannel = (UVChannel)EditorGUILayout.EnumPopup("Channel", uvChannel);
			if (EditorGUI.EndChangeCheck())
			{
				if (meshSource != null)
				{
					meshSource.Mesh.GetUVs((int)uvChannel, uvBuffer);

					if (uvBuffer.Count == 0)
						host.ShowNotification(new GUIContent($"No {uvChannel.ToString()} found."));
					else
						host.RemoveNotification();
				}
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Texture", EditorStyles.boldLabel);
			EditorGUI.indentLevel++;

			EditorGUI.BeginChangeCheck();
			UVWindowSettings.textureAlpha.value = EditorGUILayout.Slider("Alpha", UVWindowSettings.textureAlpha, 0f, 1f);
			if (EditorGUI.EndChangeCheck())
				previewMaterial.SetFloat("_Alpha", UVWindowSettings.textureAlpha);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Color");

			EditorGUI.BeginChangeCheck();
			colorChannel = (ColorChannel)GUILayout.Toolbar((int)colorChannel, colorChannelLabels, GUI.skin.button, GUI.ToolbarButtonSize.FitToContents);
			if (EditorGUI.EndChangeCheck())
				GetPreviewMaterialColorMask();

			EditorGUILayout.EndHorizontal();

			using (new EditorGUI.DisabledScope(meshSource.HasMaterial == false))
			{
				if (meshSource.HasMaterial)
				{
					string[] names = meshSource.Material.GetTexturePropertyNames()
						.Where(x => meshSource.Material.HasProperty(x)).ToArray();

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

		private Vector4 GetPreviewMaterialColorMask()
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
			return colorMask;
		}

		public void DrawPreviewTexture(Material sourceMaterial, Vector2 position, float scale)
		{
			Texture texture = sourceMaterial.GetTexture(texturePropertyName);

			if (texture != null)
			{
				Vector2 textureOffset = sourceMaterial.GetTextureOffset(texturePropertyName);
				Vector2 textureScale = sourceMaterial.GetTextureScale(texturePropertyName);
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

		public enum UVChannel
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

		public enum ColorChannel
		{
			All,
			R,
			G,
			B
		}
	}
}
