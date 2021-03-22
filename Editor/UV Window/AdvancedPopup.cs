// Copyright (c) 2021 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

namespace Nementic.MeshDebugging.UV
{
	using System;
	using UnityEditor;
	using UnityEngine;

	public static class AdvancedPopup
	{
		public static void Draw(string label, GUIContent buttonName, Action<GenericMenu> fillMenu)
		{
			Rect rect = EditorGUILayout.GetControlRect();
			rect = EditorGUI.PrefixLabel(rect, new GUIContent(label));

			if (EditorGUI.DropdownButton(
				rect, buttonName, FocusType.Keyboard))
			{
				var menu = new GenericMenu();
				fillMenu.Invoke(menu);
				menu.DropDown(rect);
			}
		}
	}
}
