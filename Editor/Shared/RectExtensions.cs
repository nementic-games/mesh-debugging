// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

namespace Nementic
{
	using UnityEngine;

	public static class RectExtensions
	{
		/// <summary>
		/// Grows the rect by <paramref name="amount"/> on all sides.
		/// Use negative values to shrink the rect instead.
		/// </summary>
		public static Rect Expand(this Rect rect, float amount)
		{
			rect.xMin -= amount;
			rect.yMin -= amount;
			rect.xMax += amount;
			rect.yMax += amount;
			return rect;
		}
	}
}
