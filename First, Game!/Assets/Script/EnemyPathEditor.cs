using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EnemyPath))]
public class EnemyPathEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EnemyPath enemy = (EnemyPath)target;

        // Draw default inspector first
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Patrol Points", EditorStyles.boldLabel);

        if (enemy.patrolPoints != null)
        {
            for (int i = 0; i < enemy.patrolPoints.Length; i++)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Point {i + 1} ({(char)('A' + i)})");

                PatrolPoint p = enemy.patrolPoints[i];

                p.point = (Transform)EditorGUILayout.ObjectField("Transform", p.point, typeof(Transform), true);
                p.waitHere = EditorGUILayout.Toggle("Wait Here", p.waitHere);
                if (p.waitHere)
                    p.waitDuration = EditorGUILayout.FloatField("Wait Duration", p.waitDuration);
                p.flipHere = EditorGUILayout.Toggle("Flip Here", p.flipHere);

                EditorGUILayout.Space();

                // Show Action toggle
                p.Action = EditorGUILayout.Toggle("Action (Pause Here)", p.Action);

                // Show Release Action button only during play mode
                if (Application.isPlaying)
                {
                    GUI.enabled = (i == enemy.CurrentIndex);
                    if (GUILayout.Button("Release Action"))
                        enemy.ReleaseAction(i);
                    GUI.enabled = true;
                }

                EditorGUILayout.EndVertical();
            }
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
        int oldLength = enemy.patrolPoints != null ? enemy.patrolPoints.Length : 0;
        PatrolPoint[] newPoints = new PatrolPoint[oldLength + 1];

        if (enemy.patrolPoints != null)
            for (int i = 0; i < oldLength; i++)
                newPoints[i] = enemy.patrolPoints[i];

        // Find or create a container for patrol points
        GameObject container = GameObject.Find("PatrolPoints");
        if (container == null)
        {
            container = new GameObject("PatrolPoints");
        }

        // Create new patrol point as child of the container
        GameObject pointObj = new GameObject($"PatrolPoint {oldLength + 1}");
        pointObj.transform.position = enemy.transform.position;
        pointObj.transform.parent = container.transform; // parented to container

        PatrolPoint newPatrolPoint = new PatrolPoint();
        newPatrolPoint.point = pointObj.transform;

        newPoints[oldLength] = newPatrolPoint;
        enemy.patrolPoints = newPoints;

        EditorUtility.SetDirty(enemy);
    }
}
