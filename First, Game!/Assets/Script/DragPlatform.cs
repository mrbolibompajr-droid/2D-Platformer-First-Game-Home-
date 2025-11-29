using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class DragPlatform : MonoBehaviour
{
    [Header("Grids (single or multiple)")]
    public GridRegion singleGrid; 
    public List<GridRegion> multipleGrids = new List<GridRegion>();

    [Header("Highlight")]
    public Color validColor = new Color(0f, 1f, 0f, 0.5f);
    public Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("Depth")]
    public float dragZ = 0f;

    [Header("Collision")]
    public LayerMask platformLayer;  // assign "Platform" layer

    private bool dragging = false;
    private Vector3 grabOffset;

    private GameObject highlightObj;
    private SpriteRenderer highlightSR;

    private static DragPlatform currentDragged;

    private Collider2D platformCollider;
    private Vector3 originalPosition;

    // ----------------------------------------------------------
    // Initialization
    // ----------------------------------------------------------
    void Start()
    {
        platformCollider = GetComponent<Collider2D>();
        dragZ = transform.position.z;

        CreateHighlight();
        HideHighlight();
    }

    void Update()
    {
        HandleInput();
        HandleDragging();
        UpdateHighlight();
    }

    // ----------------------------------------------------------
    // Grid helpers
    // ----------------------------------------------------------
    private GridRegion GetActiveGrid(Vector2 pos)
    {
        if (multipleGrids.Count > 0)
        {
            foreach (var g in multipleGrids)
                if (g != null && g.GetRegionRect().Contains(pos))
                    return g;

            return multipleGrids[0];
        }
        return singleGrid;
    }

    private Vector2 ClampToGrids(Vector2 pos)
    {
        GridRegion grid = GetActiveGrid(pos);
        if (grid == null) return pos;

        Rect r = grid.GetRegionRect();
        Bounds b = platformCollider.bounds;
        Vector2 half = b.extents;

        float x = Mathf.Clamp(pos.x, r.xMin + half.x, r.xMax - half.x);
        float y = Mathf.Clamp(pos.y, r.yMin + half.y, r.yMax - half.y);

        return new Vector2(x, y);
    }

    // Snap by bottom-left corner ✔ fixes offsets
    private Vector2 SnapPlatformToGrid(Vector2 pos, float gridSize)
    {
        Vector2 half = platformCollider.bounds.extents;

        Vector2 bottomLeft = pos - half;

        float x = Mathf.Round(bottomLeft.x / gridSize) * gridSize;
        float y = Mathf.Round(bottomLeft.y / gridSize) * gridSize;

        return new Vector2(x, y) + half;
    }

    // ----------------------------------------------------------
    // Input
    // ----------------------------------------------------------
    private void HandleInput()
    {
        if (Camera.main == null || platformCollider == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane dragPlane = new Plane(Vector3.forward, new Vector3(0, 0, dragZ));

            if (dragPlane.Raycast(ray, out float distance))
            {
                Vector3 mouseWorld = ray.GetPoint(distance);
                RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);

                if (hit.collider == platformCollider)
                {
                    dragging = true;
                    currentDragged = this;
                    originalPosition = transform.position;
                    grabOffset = transform.position - mouseWorld;

                    ShowHighlight();
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && dragging)
        {
            dragging = false;
            SnapToGrid();
            HideHighlight();

            if (currentDragged == this)
                currentDragged = null;
        }
    }

    // ----------------------------------------------------------
    // Drag movement
    // ----------------------------------------------------------
    private void HandleDragging()
    {
        if (!dragging || Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane dragPlane = new Plane(Vector3.forward, new Vector3(0, 0, dragZ));

        if (dragPlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorld = ray.GetPoint(distance);
            Vector3 desired = mouseWorld + grabOffset;

            Vector2 clamped = ClampToGrids(desired);
            GridRegion grid = GetActiveGrid(clamped);
            if (grid == null) return;

            Vector2 snapped = SnapPlatformToGrid(clamped, grid.gridSize);

            // While dragging, NEVER revert — this fixes your highlight bug
            transform.position = new Vector3(snapped.x, snapped.y, dragZ);
        }
    }

    // ----------------------------------------------------------
    // Final placement
    // ----------------------------------------------------------
    private void SnapToGrid()
    {
        Vector2 clamped = ClampToGrids(transform.position);
        GridRegion grid = GetActiveGrid(clamped);
        if (grid == null) return;

        Vector2 snapped = SnapPlatformToGrid(clamped, grid.gridSize);

        // Overlap test for actual placement
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            snapped,
            platformCollider.bounds.size,
            0f,
            platformLayer
        );

        bool canPlace = true;
        foreach (var hit in hits)
            if (hit.gameObject != this.gameObject)
                canPlace = false;

        transform.position = canPlace ? 
            new Vector3(snapped.x, snapped.y, dragZ) : 
            originalPosition;
    }

    // ----------------------------------------------------------
    // Highlight
    // ----------------------------------------------------------
    private void CreateHighlight()
    {
        highlightObj = new GameObject("Highlight");
        highlightObj.transform.parent = transform;
        highlightObj.transform.localPosition = Vector3.zero;

        highlightSR = highlightObj.AddComponent<SpriteRenderer>();
        highlightSR.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        highlightSR.drawMode = SpriteDrawMode.Sliced;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        highlightSR.sortingOrder = sr != null ? sr.sortingOrder + 1 : 100;

        highlightSR.size = platformCollider.bounds.size;
    }

    private void ShowHighlight() => highlightObj.SetActive(true);
    private void HideHighlight() => highlightObj.SetActive(false);

    private void UpdateHighlight()
    {
        if (!dragging || highlightSR == null) return;

        Vector2 clamped = ClampToGrids(transform.position);
        GridRegion grid = GetActiveGrid(clamped);
        if (grid == null) return;

        Vector2 snapped = SnapPlatformToGrid(clamped, grid.gridSize);
        Vector2 size = platformCollider.bounds.size;

        // Check for overlaps
        Collider2D[] hits = Physics2D.OverlapBoxAll(snapped, size, 0f, platformLayer);

        bool valid = true;
        foreach (var hit in hits)
            if (hit.gameObject != this.gameObject)
                valid = false;

        highlightSR.color = valid ? validColor : invalidColor;

        highlightObj.transform.position = new Vector3(snapped.x, snapped.y, dragZ - 0.01f);
        highlightSR.size = size;
    }
}
