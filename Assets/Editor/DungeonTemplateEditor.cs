// Editor/DungeonTemplateEditor.cs
using DungeonGeneration;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DungeonTemplate))]
public class DungeonTemplateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        if (GUILayout.Button("Ouvrir l'éditeur d'arbre", GUILayout.Height(30)))
            DungeonTreeEditorWindow.Open();
    }
}