// Editor/DungeonTreeEditorWindow.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using DungeonGeneration;

public class DungeonTreeEditorWindow : EditorWindow
{
    private DungeonTemplate _target;
    private Vector2 _scroll;
    private Dictionary<RoomSequenceNode, bool> _foldouts = new();

    [MenuItem("Window/Dungeon Tree Editor")]
    public static void Open() => GetWindow<DungeonTreeEditorWindow>("Dungeon Tree");

    // Ouvre automatiquement quand on sélectionne un DungeonTemplate
    [UnityEditor.Callbacks.OnOpenAsset]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceID) as DungeonTemplate;
        if (obj == null) return false;
        var w = GetWindow<DungeonTreeEditorWindow>("Dungeon Tree");
        w._target = obj;
        return true;
    }

    private void OnSelectionChange()
    {
        var sel = Selection.activeObject as DungeonTemplate;
        if (sel != null) { _target = sel; Repaint(); }
    }

    private void OnGUI()
    {
        if (_target == null)
        {
            EditorGUILayout.HelpBox("Sélectionne un DungeonTemplate dans le Project.", MessageType.Info);
            _target = (DungeonTemplate)EditorGUILayout.ObjectField("Template", _target, typeof(DungeonTemplate), false);
            return;
        }

        EditorGUILayout.LabelField(_target.name, EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        if (_target.rootNode == null)
        {
            if (GUILayout.Button("+ Créer Root Node", GUILayout.Height(30)))
            {
                Undo.RecordObject(_target, "Create Root");
                _target.rootNode = new RoomSequenceNode();
                EditorUtility.SetDirty(_target);
            }
        }
        else
        {
            DrawNode(_target.rootNode, 0, null, -1);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawNode(RoomSequenceNode node, int depth, List<ChildConnection> parentList, int indexInParent)
    {
        if (node == null) return;

        if (!_foldouts.ContainsKey(node)) _foldouts[node] = true;

        float indent = depth * 20f;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(indent);

        // Foldout
        string arrow = _foldouts[node] ? "▼" : "▶";
        if (GUILayout.Button(arrow, GUILayout.Width(20), GUILayout.Height(20)))
            _foldouts[node] = !_foldouts[node];

        // RoomType
        EditorGUI.BeginChangeCheck();
        var newType = (RoomType)EditorGUILayout.EnumPopup(node.type, GUILayout.Width(120));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_target, "Change RoomType");
            node.type = newType;
            EditorUtility.SetDirty(_target);
        }

        // Bouton supprimer (sauf root)
        if (parentList != null)
        {
            if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(20)))
            {
                Undo.RecordObject(_target, "Remove Node");
                parentList.RemoveAt(indexInParent);
                EditorUtility.SetDirty(_target);
                EditorGUILayout.EndHorizontal();
                return;
            }
        }

        // Bouton ajouter enfant
        if (GUILayout.Button("+ Enfant", GUILayout.Width(70), GUILayout.Height(20)))
        {
            Undo.RecordObject(_target, "Add Child");
            node.children.Add(new ChildConnection { child = new RoomSequenceNode() });
            _foldouts[node] = true;
            EditorUtility.SetDirty(_target);
        }

        EditorGUILayout.EndHorizontal();

        // Enfants
        if (_foldouts[node])
        {
            for (int i = node.children.Count - 1; i >= 0; i--)
            {
                var conn = node.children[i];

                // isCriticalLink
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(indent + 24);
                EditorGUI.BeginChangeCheck();
                //bool crit = EditorGUILayout.ToggleLeft("Critical", conn.isCriticalLink, GUILayout.Width(70));
                //if (EditorGUI.EndChangeCheck())
                //{
                    //Undo.RecordObject(_target, "Toggle Critical");
                    //conn.isCriticalLink = crit;
                    //EditorUtility.SetDirty(_target);
                //}
                EditorGUILayout.EndHorizontal();

                DrawNode(conn.child, depth + 1, node.children, i);
            }
        }
    }
}