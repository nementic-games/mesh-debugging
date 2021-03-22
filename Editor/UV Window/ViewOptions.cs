// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

namespace Nementic.MeshDebugging.UV
{
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;

	/// <summary>
	/// A section that configures how a <see cref="SelectedMesh"/>
	/// is displayed in the window, e.g. what color the UVs are drawn with.
	/// </summary>
	[System.Serializable]
	public class OptionsPanel
	{
		public float normalizedPosition;

		private bool isResizing;
		private Vector2 scrollPosition;

		public void Draw(Rect rect, IEnumerable<SelectedMesh> targets, List<Vector2> uvBuffer, EditorWindow host)
		{
			Rect leftBorder = rect;
			leftBorder.width = 1;

			Rect cursorRect = rect;
			cursorRect.width = 10;
			cursorRect.x -= 5;

			if (Event.current.type == EventType.MouseDown && cursorRect.Contains(Event.current.mousePosition))
			{
				isResizing = true;
				Event.current.Use();
			}

			if (isResizing && Event.current.type == EventType.MouseDrag)
			{
				normalizedPosition = Event.current.mousePosition.x / EditorGUIUtility.currentViewWidth;
				Event.current.Use();

				if (Event.current.type == EventType.MouseDrag)
					host.Repaint();
			}

			if (Event.current.type == EventType.MouseUp)
				isResizing = false;

			EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.ResizeHorizontal);

			rect = rect.Expand(-4);

			GUILayout.BeginArea(rect);
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			foreach (var target in targets)
				target.DrawOptions(uvBuffer);
			EditorGUILayout.EndScrollView();
			GUILayout.EndArea();

			EditorGUI.DrawRect(leftBorder, new Color32(35, 35, 35, 255));
		}
	}

	public enum UVChannel
	{
		None = -1,
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
		RGB,
		R,
		G,
		B
	}
}
