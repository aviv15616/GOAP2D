using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class YSort : MonoBehaviour
{
    [Tooltip("Higher value = stronger separation between Y levels.")]
    public int pixelsPerUnit = 100;

    [Tooltip("Optional offset if pivot isn't at the 'feet' point.")]
    public float yOffset = 0f;

    SortingGroup _group;
    SpriteRenderer _sr;

    void Awake()
    {
        _group = GetComponent<SortingGroup>();
        _sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        float y = transform.position.y + yOffset;
        int order = -(int)(y * pixelsPerUnit);

        if (_group != null) _group.sortingOrder = order;
        else if (_sr != null) _sr.sortingOrder = order;
    }
}
