using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Checklist))]
public class ChecklistEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Checklist checklist = (Checklist)target;

        EditorGUILayout.LabelField("Checklist", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        for (int i = 0; i < checklist.items.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            checklist.items[i].done = EditorGUILayout.Toggle(checklist.items[i].done, GUILayout.Width(20));
            checklist.items[i].name = EditorGUILayout.TextField(checklist.items[i].name);

            if (GUILayout.Button("−", GUILayout.Width(20)))
            {
                checklist.items.RemoveAt(i);
                GUI.FocusControl(null);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("+ Add Item"))
        {
            checklist.items.Add(new ChecklistItem());
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(checklist);
        }
    }
}
