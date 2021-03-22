// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

namespace Nementic.MeshDebugging.UV
{
	using System.Collections.Generic;
	using System.Linq;
	using UnityEditor;
	using UnityEngine;

	/// <summary>
	/// Returns all currently inspected mesh targets.
	/// </summary>
	[System.Serializable]
	public class SelectionManager
	{
		public IEnumerable<SelectedMesh> Meshes => meshes;

		[SerializeField]
		private List<SelectedMesh> meshes = new List<SelectedMesh>();

		[SerializeField]
		private ColorProvider colorProvider = new ColorProvider();

		private EditorWindow host;

		public void OnEnable(EditorWindow host)
		{
			this.host = host;

			foreach (var target in meshes)
			{
				if (target != null)
				{
					target.OnEnable();
					target.Repaint += host.Repaint;
				}
			}
		}

		public void OnDestroy()
		{
			foreach (var target in meshes)
			{
				if (target != null)
				{
					if (host != null)
						target.Repaint -= host.Repaint;

					target.OnDestroy();
				}
			}
		}

		public void Refresh(IEnumerable<GameObject> gameObjects)
		{
			colorProvider.Reset();

			var renderers = new HashSet<Renderer>();

			// Targets are displayed in the order they are selected.
			// If multiple targets are selected in one go, order by the hierarchy.
			foreach (GameObject go in gameObjects.OrderBy(x => x.transform.GetSiblingIndex()))
			{
				foreach (var renderer in SearchForComponents(go))
				{
					renderers.Add(renderer);
				}
			}

			for (int i = meshes.Count - 1; i >= 0; i--)
			{
				if (renderers.Remove(meshes[i].Target) == false)
				{
					meshes[i].OnDestroy();
					meshes.RemoveAt(i);
				}
			}

			foreach (var renderer in renderers)
			{
				if (TryCreateTarget(renderer, out SelectedMesh meshTarget))
					meshes.Add(meshTarget);
			}

			foreach (var mesh in meshes)
				mesh.uvColor = colorProvider.Next();
		}

		private static Renderer[] SearchForComponents(GameObject go)
		{
			if (UVWindowSettings.searchChildren)
				return go.GetComponentsInChildren<Renderer>(includeInactive: true);
			else
				return go.GetComponents<Renderer>();
		}

		private bool TryCreateTarget(Renderer renderer, out SelectedMesh selectedMesh)
		{
			switch (renderer)
			{
				case MeshRenderer meshRenderer:
				{
					var meshFilter = meshRenderer.GetComponent<MeshFilter>();
					if (meshFilter != null)
					{
						selectedMesh = new SelectedMesh(
							renderer,
							meshFilter.sharedMesh,
							meshRenderer.sharedMaterial);
						selectedMesh.Repaint += host.Repaint;
						return true;
					}
					break;
				}
				case SkinnedMeshRenderer skinnedMeshRenderer:
					selectedMesh = new SelectedMesh(
						renderer,
						skinnedMeshRenderer.sharedMesh,
						skinnedMeshRenderer.sharedMaterial);
					selectedMesh.Repaint += host.Repaint;
					return true;
			}
			selectedMesh = null;
			return false;
		}

		public void DrawInfoLabel()
		{
			Rect rect = EditorGUILayout.GetControlRect();
			rect.y -= 1;
			rect.xMin += 2;

			string label = "<Nothing Selected>";

			if (meshes != null && meshes.Count > 0)
				label = "Selected Meshes: " + meshes.Count;

			GUI.Label(rect, label, EditorStyles.boldLabel);
		}
	}
}
