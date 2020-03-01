// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

#if UNITY_EDITOR
namespace Nementic.MeshDebugging.UV
{
	using UnityEditor;
	using UnityEngine;
	using UnityEngine.Rendering;

	public static class GraphBackground
	{
		private static readonly Color kGridMinorColorDark = new Color(0f, 0f, 0f, 0.18f);
		private static readonly Color kGridMajorColorDark = new Color(0f, 0f, 0f, 0.28f);
		private static readonly Color kGridMinorColorLight = new Color(0f, 0f, 0f, 0.1f);
		private static readonly Color kGridMajorColorLight = new Color(0f, 0f, 0f, 0.15f);
		private static GUIStyle labelStyle;

		private static Color gridMinorColor
		{
			get
			{
				if (EditorGUIUtility.isProSkin)
					return kGridMinorColorDark;
				else
					return kGridMinorColorLight;
			}
		}

		private static Color gridMajorColor
		{
			get
			{
				if (EditorGUIUtility.isProSkin)
					return kGridMajorColorDark;
				else
					return kGridMajorColorLight;
			}
		}

		public static void DrawGraphBackground(Rect position, Rect graphExtents, float unitScale)
		{
			UnityEditor.Graphs.Styles.graphBackground.Draw(position, false, false, false, false);

			if (Mathf.Approximately(unitScale, 0f))
			{
				Debug.LogError("Cannot draw graph background without a scale factor.");
				return;
			}
			DrawGrid(graphExtents, unitScale);
		}

		private static void DrawGrid(Rect graphExtents, float size)
		{
			HandleUtility.ApplyWireMaterial();
			GL.PushMatrix();
			GL.Begin(1);
			DrawGridLines(graphExtents, size * 0.1f, gridMinorColor);
			DrawGridLines(graphExtents, size, gridMajorColor);
			GL.End();
			GL.PopMatrix();
			DrawGridLabels(graphExtents, size);
		}

		private static void DrawGridLines(Rect graphExtents, float gridSize, Color gridColor)
		{
			GL.Color(gridColor);
			for (float x = graphExtents.xMin; x <= graphExtents.xMax + 1f; x += gridSize)
			{
				DrawLine(new Vector2(x, graphExtents.yMin), new Vector2(x, graphExtents.yMax));
			}
			GL.Color(gridColor);
			for (float y = graphExtents.yMin; y <= graphExtents.yMax + 1f; y += gridSize)
			{
				DrawLine(new Vector2(graphExtents.xMin, y), new Vector2(graphExtents.xMax, y));
			}
		}

		private static void DrawGridLabels(Rect graphExtents, float gridSize)
		{
			if (labelStyle == null)
			{
				labelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
				{
					alignment = TextAnchor.MiddleRight,
					padding = new RectOffset(0, 4, 8, 0)
				};
			}

			for (float x = graphExtents.xMin; x < graphExtents.xMax; x += gridSize)
			{
				Vector2 position = new Vector2(x, graphExtents.center.y) - graphExtents.center;
				position /= gridSize;
				Vector2Int coord = new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));

				Handles.Label(new Vector2(x, graphExtents.center.y), coord.x.ToString(), labelStyle);
			}

			for (float y = graphExtents.yMin; y < graphExtents.yMax; y += gridSize)
			{
				Vector2 position = new Vector2(graphExtents.center.x, y) - graphExtents.center;
				position /= gridSize;
				Vector2Int coord = new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(-position.y));

				if (coord.y != 0)
					Handles.Label(new Vector2(graphExtents.center.x, y), coord.y.ToString(), labelStyle);
			}
		}

		private static void DrawLine(Vector2 p1, Vector2 p2)
		{
			GL.Vertex(p1);
			GL.Vertex(p2);
		}

		public static void ApplyWireMaterial() => HandleUtility.ApplyWireMaterial();

		private static class HandleUtility
		{
			private static Material s_HandleWireMaterial;
			private static Material s_HandleWireMaterial2D;

			internal static void ApplyWireMaterial(CompareFunction zTest = CompareFunction.Always)
			{
				Material handleWireMaterial = HandleUtility.handleWireMaterial;
				handleWireMaterial.SetInt("_HandleZTest", (int)zTest);
				handleWireMaterial.SetPass(0);
			}

			private static Material handleWireMaterial
			{
				get
				{
					InitHandleMaterials();
					return (!Camera.current) ? s_HandleWireMaterial2D : s_HandleWireMaterial;
				}
			}

			private static void InitHandleMaterials()
			{
				if (!s_HandleWireMaterial)
				{
					s_HandleWireMaterial = (Material)EditorGUIUtility.LoadRequired("SceneView/HandleLines.mat");
					s_HandleWireMaterial2D = (Material)EditorGUIUtility.LoadRequired("SceneView/2DHandleLines.mat");
				}
			}
		}
	}
}
#endif
