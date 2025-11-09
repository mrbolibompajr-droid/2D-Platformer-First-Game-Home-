using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnemyPath))]
public class EnemyPathEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EnemyPath enemy = (EnemyPath)target;

        if (enemy.patrolPoints == null)
            enemy.patrolPoints = new PatrolPoint[0];

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Patrol Points", EditorStyles.boldLabel);

        ActionManager manager = enemy.GetComponent<ActionManager>();
        if (manager == null)
            EditorGUILayout.HelpBox("No ActionManager found on this GameObject.", MessageType.Warning);

        for (int i = 0; i < enemy.patrolPoints.Length; i++)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Point {i} ({(char)('A' + i)})");

            PatrolPoint p = enemy.patrolPoints[i];

            p.point = (Transform)EditorGUILayout.ObjectField("Transform", p.point, typeof(Transform), true);
            p.waitHere = EditorGUILayout.Toggle("Wait Here", p.waitHere);
            if (p.waitHere)
                p.waitDuration = EditorGUILayout.FloatField("Wait Duration", p.waitDuration);
            p.flipHere = EditorGUILayout.Toggle("Flip Here", p.flipHere);

            bool prevAction = p.Action;
            p.Action = EditorGUILayout.Toggle("Action (Pause Here)", p.Action);

            if (p.point != null)
            {
                string suffix = "";
                if (p.Action) suffix = "_Action";
                else if (p.waitHere) suffix = "_Wait";
                p.point.name = $"Point{i}{suffix}";
            }

            if (manager != null && !Application.isPlaying)
            {
                if (p.Action && !manager.actions.Exists(a => a.patrolIndex == i))
                {
                    var action = new ActionManager.ActionData { patrolIndex = i };
                    manager.actions.Add(action);
                    EditorUtility.SetDirty(manager);
                }

                if (!p.Action && prevAction)
                {
                    var existing = manager.actions.Find(a => a.patrolIndex == i);
                    if (existing != null)
                    {
                        manager.actions.Remove(existing);
                        EditorUtility.SetDirty(manager);
                    }
                }
            }

            if (Application.isPlaying)
            {
                GUI.enabled = (i == enemy.CurrentIndex);
                if (GUILayout.Button("Release Action"))
                    enemy.ReleaseAction(i);
                GUI.enabled = true;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Point"))
            AddPoint(enemy);

        if (GUILayout.Button("Clear All Points"))
        {
            if (EditorUtility.DisplayDialog("Clear All Points?",
                "Are you sure you want to remove all patrol points?", "Yes", "No"))
            {
                enemy.patrolPoints = new PatrolPoint[0];
                if (manager != null) manager.actions.Clear();

                GameObject container = GameObject.Find("__PatrolPoints__");
                if (container != null)
                {
                    while (container.transform.childCount > 0)
                        DestroyImmediate(container.transform.GetChild(0).gameObject);
                }

                EditorUtility.SetDirty(enemy);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(enemy);
            if (manager != null) EditorUtility.SetDirty(manager);
        }
    }

    private void AddPoint(EnemyPath enemy)
    {
        int oldLength = enemy.patrolPoints.Length;
        PatrolPoint[] newPoints = new PatrolPoint[oldLength + 1];
        for (int i = 0; i < oldLength; i++)
            newPoints[i] = enemy.patrolPoints[i];

        newPoints[oldLength] = new PatrolPoint();

        GameObject container = GameObject.Find("__PatrolPoints__");
        if (container == null)
            container = new GameObject("__PatrolPoints__");

        GameObject newPoint = new GameObject($"Point{oldLength}");
        newPoint.transform.position = enemy.transform.position;
        newPoint.transform.parent = container.transform;
        newPoints[oldLength].point = newPoint.transform;

        enemy.patrolPoints = newPoints;
        EditorUtility.SetDirty(enemy);
    }
}
