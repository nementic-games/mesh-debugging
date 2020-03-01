// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

namespace Nementic.MeshDebugging.UV
{
	using UnityEditor;
	using UnityEngine;

	[System.Serializable]
	public class MeshSource
	{
		public Mesh Mesh
		{
			get => mesh;
			set
			{
				mesh = value;
				hostWindow.Repaint();
			}
		}

		public Material Material => material;

		public bool HasMaterial => material != null;

		private Mesh mesh;
		private MeshSourceMode mode;
		private Material material;
		private GameObject gameObject;
		private EditorWindow hostWindow;

		public MeshSource(EditorWindow host)
		{
			hostWindow = host;
		}

		public void DrawOptionPicker()
		{
			EditorGUI.BeginChangeCheck();
			mode = (MeshSourceMode)EditorGUILayout.EnumPopup(mode, EditorStyles.toolbarDropDown, GUILayout.Width(100));
			if (EditorGUI.EndChangeCheck())
				Refresh();

			Rect rect = EditorGUILayout.GetControlRect();
			rect.y -= 1;
			rect.xMin += 2;

			switch (mode)
			{
				case MeshSourceMode.MeshAsset:
					this.mesh = EditorGUI.ObjectField(rect, this.mesh, typeof(Mesh), allowSceneObjects: false) as Mesh;
					break;

				case MeshSourceMode.GameObject:
					EditorGUI.BeginChangeCheck();
					this.gameObject = EditorGUI.ObjectField(rect, this.gameObject, typeof(GameObject), allowSceneObjects: true) as GameObject;
					if (EditorGUI.EndChangeCheck())
						Refresh();
					break;

				case MeshSourceMode.Selection:
					string label = mesh != null ? mesh.name : "<Nothing Selected>";
					GUI.Label(rect, label, EditorStyles.boldLabel);
					break;
			}
		}

		public void Refresh()
		{
			switch (mode)
			{
				case MeshSourceMode.GameObject:
					TryGetMesh(this.gameObject);
					break;

				case MeshSourceMode.Selection:
					TryGetMesh(Selection.activeObject);
					break;
			}
		}

		private void TryGetMesh(Object target)
		{
			if (target == null)
				this.mesh = null;

			if (target is Mesh mesh)
				this.mesh = mesh;

			if (target is GameObject gameObject)
			{
				if (gameObject == null)
				{
					material = null;
					mesh = null;
					return;
				}
				var meshFilter = gameObject.GetComponentInChildren<MeshFilter>();

				// TODO: Handle conflicting components by showing a dropdown
				// or a list of bubble buttons at the top?

				if (meshFilter != null)
				{
					var renderer = meshFilter.GetComponent<Renderer>();
					if (renderer != null)
						material = renderer.sharedMaterial;

					this.mesh = meshFilter.sharedMesh;
				}
				else
				{
					var skinnedMeshRenderer = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
					if (skinnedMeshRenderer != null)
					{
						material = skinnedMeshRenderer.sharedMaterial;

						this.mesh = skinnedMeshRenderer.sharedMesh;
					}
				}
			}
		}

		public static implicit operator Mesh(MeshSource meshSource) => meshSource.mesh;
	}

	public enum MeshSourceMode
	{
		Selection,
		MeshAsset,
		GameObject
	}
}
