// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

namespace Nementic.MeshDebugging.UV
{
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEditor.SettingsManagement;
	using UnityEditorInternal;
	using UnityEngine;
	using UnityEngine.Accessibility;

	/// <summary>
	/// Persistent user settings of the <see cref="UVWindow"/>.
	/// </summary>
	public static class UVWindowSettings
	{
		public static readonly UserSetting<bool> showOptions;
		
		public static readonly UserSetting<bool> searchChildren;

		[UserSetting("UV", "Alpha")]
		public static readonly UserSetting<float> uvAlphaDefault;

		/// <summary>
		/// The RGB color components (alpha is ignored) to display the UV mesh in.
		/// </summary>
		[UserSetting]
		public static readonly UserSetting<List<Color>> uvColorDefaults;

		[UserSetting("Texture", "Alpha")]
		public static readonly UserSetting<float> textureAlphaDefault;

		[UserSetting]
		public static readonly UserSetting<float> zoomSpeed;

		[UserSetting]
		public static readonly UserSetting<float> minZoom;

		[UserSetting]
		public static readonly UserSetting<float> maxZoom;

		private static readonly Settings settings;
		private static readonly ReorderableList colorList;
		private static Color[] randomColors;
		private static int randomColorIndex;

		public static float PerformZoom(float zoom, float delta)
		{
			zoom -= delta * zoomSpeed;
			zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
			return zoom;
		}

		static UVWindowSettings()
		{
			// Using the static constructor is important here,
			// because the properties may be accessed by the SettingsProvider
			// before they are initialized otherwise.

			settings = new Settings("com.nementic.mesh-debugging", "UVWindowSettings");

			showOptions = new WindowSetting<bool>("showOptions", false);
			searchChildren = new WindowSetting<bool>("searchChildren", true);
			uvAlphaDefault = new WindowSetting<float>("uvAlpha", 1f);
			uvColorDefaults = new WindowSetting<List<Color>>("uvColors", new List<Color>
			{
				new Color32(36, 233, 233, 255),
				new Color32(248, 219, 78, 255),
				new Color32(103, 233, 54, 255),
				new Color32(238, 72, 42, 255),
			});
			textureAlphaDefault = new WindowSetting<float>("textureAlpha", 1f);
			zoomSpeed = new WindowSetting<float>("zoomSpeed", 0.1f);
			minZoom = new WindowSetting<float>("minZoom", 0.5f);
			maxZoom = new WindowSetting<float>("maxZoom", 10f);

			colorList = new ReorderableList(uvColorDefaults.value, typeof(Color));
			colorList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Default Colors");
			colorList.drawElementCallback = DrawListElement;
			colorList.onCanRemoveCallback = list => list.count > 1;
			colorList.onAddCallback = AddListElement;
		}

		private static void AddListElement(ReorderableList list)
		{
			if (randomColors == null)
			{
				randomColors = new Color[16];
				VisionUtility.GetColorBlindSafePalette(randomColors, 0.5f, 0.85f);
			}
			uvColorDefaults.value.Add(randomColors[randomColorIndex % randomColors.Length]);
			randomColorIndex++;
		}

		private static void DrawListElement(Rect rect, int index, bool isactive, bool isfocused)
		{
			rect.height = EditorGUIUtility.singleLineHeight;
			rect.y += 2;

			// When the array setting is reset.
			if (!ReferenceEquals(colorList.list, uvColorDefaults.value) ||
			    index >= uvColorDefaults.value.Count)
			{
				colorList.list = uvColorDefaults.value;
				return;
			}

			uvColorDefaults.value[index] = EditorGUI.ColorField(
				rect, GUIContent.none, uvColorDefaults.value[index],
				showEyedropper: true, showAlpha: false, hdr: false);
		}

		private class WindowSetting<T> : UserSetting<T>
		{
			public WindowSetting(string key, T value, SettingsScope scope = SettingsScope.User)
				: base(UVWindowSettings.settings, key, value, scope) { }
		}

		[SettingsProvider]
		private static SettingsProvider CreateSettingsProvider()
		{
			return new UserSettingsProvider("Nementic/UV Window",
				settings,
				new[] { typeof(UVWindow).Assembly });
		}

		[UserSettingBlock("Graph Zoom")]
		private static void GraphZoomSettings(string searchContext)
		{
			EditorGUI.BeginChangeCheck();

			zoomSpeed.value = SettingsGUILayout.SettingsSlider("Speed", zoomSpeed, -1f, 1f, searchContext);
			minZoom.value = SettingsGUILayout.SettingsSlider("Minimum", minZoom, 0.01f, 1f, searchContext);
			maxZoom.value = SettingsGUILayout.SettingsSlider("Maximum", maxZoom, 1f, 100f, searchContext);

			if (EditorGUI.EndChangeCheck())
			{
				zoomSpeed.ApplyModifiedProperties();
				minZoom.ApplyModifiedProperties();
				maxZoom.ApplyModifiedProperties();
			}
		}

		[UserSettingBlock("UV")]
		private static void ColorDefaults(string searchContext)
		{
			EditorGUI.BeginChangeCheck();
			GUILayout.Space(2f);
			colorList.DoLayoutList();
			SettingsGUILayout.DoResetContextMenuForLastRect(uvColorDefaults);
			if (EditorGUI.EndChangeCheck())
				uvColorDefaults.ApplyModifiedProperties();
		}
	}
}
