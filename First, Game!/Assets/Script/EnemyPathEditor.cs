using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnemyPath))]
public class EnemyPathEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EnemyPath enemy = (EnemyPath)target;

        // Ensure patrolPoints is initialized
        if (enemy.patrolPoints == null)
            enemy.patrolPoints = new PatrolPoint[0];

        // Draw default inspector first
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Patrol Points", EditorStyles.boldLabel);

        // Draw patrol points
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
            p.Action = EditorGUILayout.Toggle("Action (Pause Here)", p.Action);

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

        // Add / clear buttons (editor only)
        if (!Application.isPlaying)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Point"))
                AddPoint(enemy);

            if (GUILayout.Button("Clear All Points"))
            {
                if (EditorUtility.DisplayDialog("Clear All Points?",
                    "Are you sure you want to remove all patrol points?", "Yes", "No"))
                {
                    enemy.patrolPoints = new PatrolPoint[0];
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // Mark object dirty if modified
        if (GUI.changed)
            EditorUtility.SetDirty(enemy);
    }

    private void AddPoint(EnemyPath enemy)
    {
        int oldLength = enemy.patrolPoints.Length;
        PatrolPoint[] newPoints = new PatrolPoint[oldLength + 1];
        for (int i = 0; i < oldLength; i++)
            newPoints[i] = enemy.patrolPoints[i];

        newPoints[oldLength] = new PatrolPoint();

        // Automatically create container in hierarchy if needed
        if (newPoints[oldLength].point == null)
        {
            GameObject container = GameObject.Find("__PatrolPoints__");
            if (container == null)
            {
                container = new GameObject("__PatrolPoints__");
            }

            GameObject newPoint = new GameObject($"Point {oldLength}");
            newPoint.transform.position = enemy.transform.position; // start at enemy
            newPoint.transform.parent = container.transform;
            newPoints[oldLength].point = newPoint.transform;
        }

        enemy.patrolPoints = newPoints;
    }
}
