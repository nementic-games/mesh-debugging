// Copyright (c) 2021 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

namespace Nementic.MeshDebugging.UV
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEditor;
	using UnityEngine;
	using Object = UnityEngine.Object;

	/// <summary>
	/// Represents a mesh that is selected for inspection
	/// and stores its associated display options.
	/// </summary>
	[Serializable]
	public class SelectedMesh
	{
		public event Action Repaint;

		public float uvAlpha = 1f;
		public Color uvColor = Color.white;
		public UVChannel uvChannel = UVChannel.UV0;

		public float TextureAlpha
		{
			get => textureAlpha;
			set
			{
				if (textureAlpha != value)
				{
					textureAlpha = value;
					previewMaterial.SetFloat(alphaPropertyID, value);
				}
			}
		}

		private float textureAlpha = 1f;

		public ColorChannel colorChannel = ColorChannel.RGB;
		public string texturePropertyName = "_MainTex";

		public Color UVColorWithAlpha
		{
			get
			{
				var color = uvColor;
				color.a = uvAlpha;
				return color;
			}
		}

		public Renderer Target { get; private set; }

		private Material previewMaterial;
		private bool foldout = true;

		private static readonly int alphaPropertyID = Shader.PropertyToID("_Alpha");
		private static readonly int isBumpMapPropertyID = Shader.PropertyToID("_IsBumpMap");
		private static readonly int colorMaskPropertyID = Shader.PropertyToID("_ColorMask");

		public SelectedMesh(Renderer target, Mesh mesh, Material material)
		{
			this.Target = target;
			this.mesh = mesh;
			this.material = material;
			CreatePreviewMaterial();
			InitializeDefaultSettings();
			OnEnable();
		}

		public void OnEnable()
		{
			Undo.postprocessModifications -= PostprocessModifications;
			Undo.postprocessModifications += PostprocessModifications;
			Undo.undoRedoPerformed += RefreshReferences;
		}

		public void OnDestroy()
		{
			if (previewMaterial != null)
			{
				Object.DestroyImmediate(previewMaterial);
				previewMaterial = null;
			}
			Undo.postprocessModifications -= PostprocessModifications;
			Undo.undoRedoPerformed -= RefreshReferences;
		}

		private UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
		{
			for (int i = 0; i < modifications.Length; i++)
			{
				Object target = modifications[i].currentValue.target;

				if (target is Component component)
				{
					if (target == Target || target == Target.GetComponent<MeshFilter>())
					{
						RefreshReferences();
					}
				}
			}
			return modifications;
		}

		private void RefreshReferences()
		{
			this.material = Target.sharedMaterial;

			if (Target is SkinnedMeshRenderer skinnedMeshRenderer)
				this.mesh = skinnedMeshRenderer.sharedMesh;
			else
			{
				var meshFilter = Target.GetComponent<MeshFilter>();
				if (meshFilter != null)
					this.mesh = meshFilter.sharedMesh;
			}
			Repaint?.Invoke();
		}

		private void CreatePreviewMaterial()
		{
			const string uvShaderPath =
				"Packages/com.nementic.mesh-debugging/Editor/UV Window/UV-Preview.shader";
			Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(uvShaderPath);
			previewMaterial = new Material(shader);
			previewMaterial.hideFlags = HideFlags.HideAndDontSave;
			UpdatePreviewMaterialColorMask();
		}

		private void InitializeDefaultSettings()
		{
			uvAlpha = UVWindowSettings.uvAlphaDefault;
			TextureAlpha = UVWindowSettings.textureAlphaDefault;

			texturePropertyName = string.Empty;
			if (material != null)
			{
				texturePropertyName = material
					.GetTexturePropertyNames()
					.Where(x => material.HasProperty(x))
					.FirstOrDefault(x => material.GetTexture(x) != null);
			}

			uvChannel = UVChannel.None;
			if (mesh != null)
			{
				var tmpBuffer = new List<Vector3>();
				for (int i = 0; i < 8; i++)
				{
					mesh.GetUVs(i, tmpBuffer);
					if (tmpBuffer.Count > 0)
					{
						uvChannel = (UVChannel)i;
						break;
					}
				}
			}
		}

		public void DrawOptions(List<Vector2> uvBuffer)
		{
			Color color = GUI.backgroundColor;
			GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
			string headerText = Target.name;
			if (mesh != null && !string.Equals(
				mesh.name, Target.name, StringComparison.CurrentCultureIgnoreCase))
			{
				headerText += " - " + mesh.name;
			}
			foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, headerText);
			GUI.backgroundColor = color;

			if (foldout)
			{
				float labelWidth = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 100;
				DrawSection("UV", () => DrawUVSection(uvBuffer));
				// Don't use EditorGUILayout.Space because that causes the header group to jitter.
				GUILayout.Space(6f);
				DrawSection("Texture", DrawTextureSection);
				EditorGUIUtility.labelWidth = labelWidth;
				GUILayout.Space(6f);
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		private void DrawSection(string name, Action drawContent)
		{
			var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
			style.alignment = TextAnchor.MiddleLeft;
			EditorGUILayout.LabelField(name, style);
			EditorGUI.indentLevel++;
			drawContent?.Invoke();
			EditorGUI.indentLevel--;
		}

		private void DrawUVSection(List<Vector2> uvBuffer)
		{
			uvAlpha = EditorGUILayout.Slider("Alpha", uvAlpha, 0f, 1f);

			uvColor = EditorGUILayout.ColorField(
				new GUIContent("Color"),
				uvColor,
				showEyedropper: true,
				showAlpha: false,
				hdr: false);

			DrawUVChannelDropdown(uvBuffer);
		}

		private void DrawUVChannelDropdown(List<Vector2> uvBuffer)
		{
			AdvancedPopup.Draw("Channel", new GUIContent(uvChannel.ToString()), menu =>
			{
				for (int i = -1; i < 8; i++)
				{
					var content = new GUIContent(((UVChannel)i).ToString());
					var tmpBuffer = new List<Vector2>();

					if (mesh != null && i != -1)
						mesh.GetUVs(i, tmpBuffer);

					if (tmpBuffer.Count == 0 && i != -1)
						menu.AddDisabledItem(content);
					else
					{
						menu.AddItem(content, false, userData =>
						{
							uvChannel = (UVChannel)userData;

							if (uvChannel != UVChannel.None)
							{
								if (TryGetMesh(out Mesh mesh))
									mesh.GetUVs((int)uvChannel, uvBuffer);
							}
						}, i);
					}
				}
			});
		}

		private void DrawTextureSection()
		{
			TextureAlpha = EditorGUILayout.Slider("Alpha", TextureAlpha, 0f, 1f);

			DrawColorChannelToolbar();

			List<string> names;

			if (material != null)
			{
				names = material.GetTexturePropertyNames()
					.Where(x => material.HasProperty(x)).ToList();
			}
			else
			{
				names = new List<string>(1);
			}

			// None option for easy toggling or if there are no available textures.
			names.Insert(0, "None");

			int selectedIndex = names.IndexOf(texturePropertyName);

			if (selectedIndex == -1)
				selectedIndex = 0;

			AdvancedPopup.Draw("Map", DisplayName(names[selectedIndex]), menu =>
			{
				menu.AddItem(new GUIContent("None"), false, () =>
				{
					texturePropertyName = string.Empty;
				});
				for (int i = 1; i < names.Count; i++)
				{
					var label = DisplayName(names[i]);
					if (material.GetTexture(names[i]) != null)
					{
						menu.AddItem(label, false, userData =>
						{
							texturePropertyName = names[(int)userData];
						}, i);
					}
					else
					{
						menu.AddDisabledItem(label);
					}
				}
			});
		}

		private static GUIContent DisplayName(string texturePropertyName)
		{
			if (texturePropertyName.StartsWith("_"))
				return new GUIContent(texturePropertyName.Substring(1, texturePropertyName.Length - 1));
			return new GUIContent(texturePropertyName);
		}

		private void DrawColorChannelToolbar()
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Channel");
			EditorGUI.BeginChangeCheck();
			colorChannel = (ColorChannel)GUILayout.Toolbar((int)colorChannel,
				Enum.GetNames(typeof(ColorChannel)),
				GUI.skin.button, GUI.ToolbarButtonSize.FitToContents);
			if (EditorGUI.EndChangeCheck())
				UpdatePreviewMaterialColorMask();
			EditorGUILayout.EndHorizontal();
		}

		private void UpdatePreviewMaterialColorMask()
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

			previewMaterial.SetVector(colorMaskPropertyID, colorMask);
		}

		public void DrawPreviewTexture(Material sourceMaterial, Vector2 position, float scale)
		{
			if (string.IsNullOrEmpty(texturePropertyName))
				return;

			Texture texture = sourceMaterial.GetTexture(texturePropertyName);

			if (texture == null)
				return;

			Vector2 textureOffset = sourceMaterial.GetTextureOffset(texturePropertyName);
			Vector2 textureScale = sourceMaterial.GetTextureScale(texturePropertyName);
			Vector2 texturePosition = position;

			texturePosition -= new Vector2(textureOffset.x, textureOffset.y) * scale /
			                   new Vector2(textureScale.x, -textureScale.y);
			texturePosition.y -= 1f / textureScale.y * scale;

			textureScale = new Vector2(scale, scale) / textureScale;

			Rect rect = new Rect(texturePosition, textureScale);

			int isBumpMap = texturePropertyName.Contains("Bump") ? 1 : 0;
			previewMaterial.SetInt(isBumpMapPropertyID, isBumpMap);

			Graphics.DrawTexture(rect, texture, previewMaterial);
		}

		public Mesh Mesh
		{
			get => mesh;
		}

		[SerializeField]
		private Mesh mesh;

		public Material Material
		{
			get => material;
		}

		[SerializeField]
		private Material material;

		public bool TryGetMesh(out Mesh mesh)
		{
			mesh = this.mesh;
			return mesh != null;
		}

		public bool TryGetMaterial(out Material material)
		{
			material = this.material;
			return material != null;
		}
	}
}
