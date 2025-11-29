using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class GridRegion : MonoBehaviour
{
    public float gridSize = 1f;
    public Vector2 regionSize = new Vector2(10, 6); // width, height in tiles

    [Header("Runtime Grid Visualization")]
    public bool showInGame = true;
    public Color lineColor = new Color(1f, 1f, 1f, 0.25f);

    private List<LineRenderer> runtimeLines = new List<LineRenderer>();

    // Get the region rectangle
    public Rect GetRegionRect()
    {
        Vector2 pos = (Vector2)transform.position - (regionSize * 0.5f);
        return new Rect(pos, regionSize);
    }

    // Clamp position to grid
    public Vector2 ClampToRegion(Vector2 pos)
    {
        Rect r = GetRegionRect();
        float x = Mathf.Clamp(pos.x, r.xMin, r.xMax);
        float y = Mathf.Clamp(pos.y, r.yMin, r.yMax);
        return new Vector2(x, y);
    }

    private void OnValidate()
    {
        SnapPositionAndSize();
        if (Application.isPlaying && showInGame)
            DrawRuntimeGrid();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            SnapPositionAndSize();
        else if (showInGame)
            DrawRuntimeGrid();
    }

    private void SnapPositionAndSize()
    {
        float x = Mathf.Round(transform.position.x / gridSize) * gridSize;
        float y = Mathf.Round(transform.position.y / gridSize) * gridSize;
        transform.position = new Vector3(x, y, transform.position.z);

        float width = Mathf.Round(regionSize.x);
        float height = Mathf.Round(regionSize.y);
        regionSize = new Vector2(Mathf.Max(1, width), Mathf.Max(1, height));
    }

    private void DrawRuntimeGrid()
    {
        ClearRuntimeLines();

        Rect r = GetRegionRect();
        for (float x = r.xMin; x <= r.xMax; x += gridSize)
            CreateLine(new Vector3(x, r.yMin, 0f), new Vector3(x, r.yMax, 0f));

        for (float y = r.yMin; y <= r.yMax; y += gridSize)
            CreateLine(new Vector3(r.xMin, y, 0f), new Vector3(r.xMax, y, 0f));
    }

    private void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject go = new GameObject("GridLine");
        go.transform.parent = transform;
        go.transform.localPosition = Vector3.zero;
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = 0.02f;
        lr.endWidth = 0.02f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.sortingOrder = 1000; // in front of most objects
        runtimeLines.Add(lr);
    }

    private void ClearRuntimeLines()
    {
        foreach (var lr in runtimeLines)
        {
            if (lr != null) Destroy(lr.gameObject);
        }
        runtimeLines.Clear();
    }

    private void OnDrawGizmos()
    {
        Rect r = GetRegionRect();
        Gizmos.color = lineColor;

        for (float x = r.xMin; x <= r.xMax; x += gridSize)
            Gizmos.DrawLine(new Vector3(x, r.yMin), new Vector3(x, r.yMax));

        for (float y = r.yMin; y <= r.yMax; y += gridSize)
            Gizmos.DrawLine(new Vector3(r.xMin, y), new Vector3(r.xMax, y));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, regionSize);
    }
}
