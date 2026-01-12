using UnityEngine;
using UnityEngine.Rendering;

public class YSort : MonoBehaviour
{
    public Collider2D targetCollider;
    public SpriteRenderer targetRenderer; // used if no SortingGroup
    public SortingGroup targetSortingGroup; // used for trees/multi-sprite stations

    public int pixelsPerUnit = 100;
    public int orderOffset = 0;

    void Awake()
    {
        if (!targetCollider)
            targetCollider = GetComponentInChildren<Collider2D>();
        if (!targetSortingGroup)
            targetSortingGroup = GetComponent<SortingGroup>();
        if (!targetRenderer)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (!targetCollider)
            return;

        float bottomY = targetCollider.bounds.min.y;

        // lower on screen (smaller Y) => bigger sortingOrder => drawn in front
        int order = Mathf.RoundToInt(-bottomY * pixelsPerUnit) + orderOffset;

        if (targetSortingGroup)
        {
            targetSortingGroup.sortingOrder = order;
        }
        else if (targetRenderer)
        {
            targetRenderer.sortingOrder = order;
        }
    }
}
