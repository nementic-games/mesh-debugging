// Copyright (c) 2021 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

namespace Nementic.MeshDebugging.UV
{
	using UnityEditor;
	using UnityEngine;

	internal static class ContextualPropertyMenu
	{
		private const string commandName = "Inspect in UV Window...";

		[InitializeOnLoadMethod]
		private static void Initialize()
		{
			// This is called when right-clicking ona property name,
			// but not on the object field directly (too bad).
			EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
		}

		[MenuItem("CONTEXT/MeshFilter/" + commandName)]
		[MenuItem("CONTEXT/MeshRenderer/" + commandName)]
		[MenuItem("CONTEXT/SkinnedMeshRenderer/" + commandName)]
		private static void OnContextMenu(MenuCommand command)
		{
			if (command.context is Component component)
				UVWindow.GetWindow().Inspect(component.gameObject);
		}

		private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
		{
			if (HasValidMeshReference(property))
			{
				var content = new GUIContent(commandName);
				var component = property.serializedObject.targetObject as Component;
				if (component != null)
				{
					menu.AddItem(content, false, () =>
					{
						UVWindow.GetWindow().Inspect(component.gameObject);
					});
				}
				else
				{
					menu.AddDisabledItem(content);
				}
			}
		}

		private static bool HasValidMeshReference(SerializedProperty property)
		{
			if (property.propertyType == SerializedPropertyType.ObjectReference)
			{
				var mesh = property.objectReferenceValue as Mesh;
				return mesh != null;
			}
			return false;
		}
	}
}
