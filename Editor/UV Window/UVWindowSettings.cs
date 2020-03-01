// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

namespace Nementic.MeshDebugging.UV
{
	using UnityEditor;
	using UnityEditor.SettingsManagement;
	using UnityEngine;

	/// <summary>
	/// Persistent user settings of the <see cref="UVWindow"/>.
	/// Note, that there other non-persistent options.
	/// </summary>
	public class UVWindowSettings
	{
		public static readonly UserSetting<bool> showOptions;

		[UserSetting("UV", "Alpha")]
		public static readonly UserSetting<float> uvAlpha;

		/// <summary>
		/// The RGB color components (alpha is ignored) to display the UV mesh in.
		/// </summary>
		[UserSetting("UV", "Color")]
		public static readonly UserSetting<Color> uvColor;

		[UserSetting("Texture", "Alpha")]
		public static readonly UserSetting<float> textureAlpha;

		public static Color UVColorWithAlpha
		{
			get
			{
				var color = uvColor.value;
				color.a = uvAlpha.value;
				return color;
			}
		}

		[UserSetting()]
		public static readonly UserSetting<float> zoomSpeed;

		[UserSetting()]
		public static readonly UserSetting<float> minZoom;

		[UserSetting()]
		public static readonly UserSetting<float> maxZoom;

		public static float PerformZoom(float zoom, float delta)
		{
			zoom -= delta * zoomSpeed;
			zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
			return zoom;
		}

		static UVWindowSettings()
		{
			// Using the static constructor is very important here,
			// because the properties may be accessed by the SettingsProvider
			// before they are initialized otherwise.

			settings = new Settings("com.nementic.mesh-debugging", "UVWindowSettings");

			showOptions = new WindowSetting<bool>("showOptions", false);
			uvAlpha = new WindowSetting<float>("uvAlpha", 1f);
			uvColor = new WindowSetting<Color>("uvColor", Color.cyan * 0.95f);
			textureAlpha = new WindowSetting<float>("textureAlpha", 1f);
			zoomSpeed = new WindowSetting<float>("zoomSpeed", 0.1f);
			minZoom = new WindowSetting<float>("minZoom", 0.5f);
			maxZoom = new WindowSetting<float>("maxZoom", 10f);
		}

		private static readonly Settings settings;

		private class WindowSetting<T> : UserSetting<T>
		{
			public WindowSetting(string key, T value, SettingsScope scope = SettingsScope.User)
				: base(UVWindowSettings.settings, key, value, scope)
			{
			}
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

			zoomSpeed.value = SettingsGUILayout.SettingsSlider("Speed", zoomSpeed, float.MinValue, float.MaxValue, searchContext);
			minZoom.value = SettingsGUILayout.SettingsSlider("Minimum", minZoom, 0.01f, 1f, searchContext);
			maxZoom.value = SettingsGUILayout.SettingsSlider("Maximum", maxZoom, 1f, 100f, searchContext);

			if (EditorGUI.EndChangeCheck())
			{
				zoomSpeed.ApplyModifiedProperties();
				minZoom.ApplyModifiedProperties();
				maxZoom.ApplyModifiedProperties();

				settings.Save();
			}
		}
	}
}