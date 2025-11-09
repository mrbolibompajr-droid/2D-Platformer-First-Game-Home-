using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(EnemyPath))]
public class EnemyPathEditor : Editor
{
    private PatrolPoint[] backupPatrolPoints = null;
    private List<GameObject> backupPointObjects = null;

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
        {
            EditorGUILayout.HelpBox("No ActionManager found on this GameObject.", MessageType.Warning);
        }

        // Display patrol points
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

            // Update GameObject name based on type
            if (p.point != null)
            {
                string suffix = "";
                if (p.Action) suffix = "_Action";
                else if (p.waitHere) suffix = "_Wait";
                p.point.name = $"Point{i}{suffix}";
            }

            // Sync ActionManager in Edit mode
            if (manager != null && !Application.isPlaying)
            {
                if (p.Action && !manager.actions.Exists(a => a.patrolIndex == i))
                {
                    var action = new ActionManager.ActionData();
                    action.patrolIndex = i;
                    manager.actions.Add(action);
                    EditorUtility.SetDirty(manager);
                    Debug.Log($"Added ActionData for patrolIndex {i}");
                }

                if (!p.Action && prevAction)
                {
                    var existing = manager.actions.Find(a => a.patrolIndex == i);
                    if (existing != null)
                    {
                        manager.actions.Remove(existing);
                        EditorUtility.SetDirty(manager);
                        Debug.Log($"Removed ActionData for patrolIndex {i}");
                    }
                }
            }

            // Release Action button in Play mode
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

        // Buttons: Add, Restore, Clear
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Point"))
            AddPoint(enemy);

        // Restore Points button
        GUI.enabled = (backupPatrolPoints != null && backupPatrolPoints.Length > 0);
        if (GUILayout.Button("Restore Points"))
        {
            // Restore patrolPoints array
            enemy.patrolPoints = backupPatrolPoints;

            // Restore GameObjects in Hierarchy
            GameObject container = GameObject.Find("__PatrolPoints__");
            if (container == null)
                container = new GameObject("__PatrolPoints__");

            foreach (GameObject go in backupPointObjects)
            {
                if (go != null)
                {
                    go.transform.parent = container.transform;
                    go.SetActive(true);
                }
            }

            // Clear backup after restoring
            backupPatrolPoints = null;
            backupPointObjects = null;

            EditorUtility.SetDirty(enemy);
        }
        GUI.enabled = true;

        // Clear All Points button
        if (GUILayout.Button("Clear All Points"))
        {
            if (EditorUtility.DisplayDialog("Clear All Points?",
                "Are you sure you want to remove all patrol points?", "Yes", "No"))
            {
                // Backup current patrol points
                backupPatrolPoints = enemy.patrolPoints;
                backupPointObjects = new List<GameObject>();
                GameObject container = GameObject.Find("__PatrolPoints__");
                if (container != null)
                {
                    foreach (Transform child in container.transform)
                    {
                        if (child != null)
                        {
                            backupPointObjects.Add(child.gameObject);
                            child.gameObject.SetActive(false); // temporarily disable
                        }
                    }
                }

                // Clear patrolPoints array
                enemy.patrolPoints = new PatrolPoint[0];

                // Clear ActionManager actions
                if (manager != null)
                {
                    manager.actions.Clear();
                    EditorUtility.SetDirty(manager);
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
