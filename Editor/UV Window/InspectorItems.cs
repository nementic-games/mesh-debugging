// Copyright (c) 2019 Nementic Games GmbH. All Rights Reserved.
// Author: Chris Yarbrough

namespace Nementic.MeshDebugging.UV
{
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    internal static class InspectorItems
    {
        static InspectorItems()
        {
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }

        private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (HasValidMeshReference(property, out Mesh mesh))
            {
                menu.AddItem(new GUIContent("Inspect in UV Window"), false, () =>
                {
                    UVWindow.GetWindow().Mesh = mesh;
                });
            }
        }

        private static bool HasValidMeshReference(SerializedProperty property, out Mesh mesh)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                mesh = property.objectReferenceValue as Mesh;
                return mesh != null;
            }
            mesh = null;
            return false;
        }
    }
}
