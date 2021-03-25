// Copyright (c) 2021 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

namespace Nementic.MeshDebugging.UV
{
	using UnityEngine;

	[System.Serializable]
	public class ColorProvider
	{
		[SerializeField]
		private int index;

		public Color Next()
		{
			var colors = UVWindowSettings.uvColorDefaults.value;

			// The GUI should ensure at least one color, but if something
			// goes wrong during serialization use a reasonable default.
			if (colors.Count == 0)
				return Color.white;

			var color = colors[index % colors.Count];

			// If there are more meshes than colors produce some variations,
			// everything else is better tweaked by the user anyway.
			int cycle = (index / colors.Count) + 1;
			if (cycle > 1)
				color *= 1f / (float)cycle;

			index++;
			return color;
		}

		public void Reset()
		{
			index = 0;
		}
	}
}
