using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class GetPath : EditorWindow
{
    List<string> objectPaths = new List<string>();

    [MenuItem("Tools/Get GameObject Paths")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(GetPath), true, "GameObject Path Finder");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Get Paths of Selected GameObject and Children"))
        {
            GetSelectedGameObjectAndChildrenPaths();
        }

        EditorGUILayout.LabelField("Paths:");
        EditorGUI.indentLevel++;
        foreach (string path in objectPaths)
        {
            EditorGUILayout.LabelField(path);
        }
        EditorGUI.indentLevel--;

        if (GUILayout.Button("Copy Paths to Clipboard"))
        {
            CopyPathsToClipboard();
        }
    }

    void OnSelectionChange()
    {
        GetSelectedGameObjectAndChildrenPaths();
        Repaint();
    }

    void GetSelectedGameObjectAndChildrenPaths()
    {
        objectPaths.Clear();
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject != null)
        {
            string basePath = GetRelativePath(selectedObject.transform);
            GetAllChildPaths(selectedObject.transform, basePath);
        }
        else
        {
            objectPaths.Add("No GameObject selected!");
        }
    }

    void GetAllChildPaths(Transform transform, string currentPath)
    {
        foreach (Transform child in transform)
        {
            string childPath = string.IsNullOrEmpty(currentPath) ? child.name : currentPath + "/" + child.name;
            objectPaths.Add(childPath);
            GetAllChildPaths(child, childPath);
        }
    }

    string GetRelativePath(Transform transform)
    {
        List<string> pathParts = new List<string>();
        Transform current = transform;
        Transform root = current.root;

        while (current != root)
        {
            pathParts.Add(current.name);
            current = current.parent;
        }

        pathParts.Reverse();
        return string.Join("/", pathParts.Skip(1)); // Skip the first element (selected object)
    }

    void CopyPathsToClipboard()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (string path in objectPaths)
        {
            Debug.Log($"Copying: {path}");
            string newPath = $"{path}";
            sb.AppendLine(newPath);
        }
        GUIUtility.systemCopyBuffer = sb.ToString();
    }
}
#endif