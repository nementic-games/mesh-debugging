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

		static UVWindowSettings()
		{
			settings = new Settings("com.nementic.mesh-debugging", "UVWindowSettings");

			showOptions = new WindowSetting<bool>("showOptions", false);
			uvAlpha = new WindowSetting<float>("uvAlpha", 1f);
			uvColor = new WindowSetting<Color>("uvColor", Color.cyan * 0.95f);
			textureAlpha = new WindowSetting<float>("textureAlpha", 1f);
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
	}
}