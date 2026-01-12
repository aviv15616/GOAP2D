using TMPro;
using UnityEngine;

public class NPCUI : MonoBehaviour
{
    public GoapAgent agent;
    public Needs needs;

    [Header("Prefab")]
    public GameObject npcDebugBubblePrefab;

    [Header("Offsets")]
    public Vector3 uiOffset = new Vector3(0f, 1.6f, 0f);

    // refs בתוך prefab
    Transform _uiRoot;
    Transform _hungerBar,
        _heatBar,
        _energyBar;
    GameObject _bubbleContainer;
    TMP_Text _bubbleText;

    Coroutine _hideCo;

    void Awake()
    {
        if (!agent)
            agent = GetComponent<GoapAgent>();
        if (!needs)
            needs = GetComponent<Needs>();

        _uiRoot = Instantiate(npcDebugBubblePrefab).transform;
        _uiRoot.name = $"{name}_OverheadUI";
    }

    void Start()
    {
        // מציאת אובייקטים לפי השמות בהיררכיה שלך
        _hungerBar = _uiRoot.Find("Meters/Hunger/green_0");
        _heatBar = _uiRoot.Find("Meters/Heat/orange_0");
        _energyBar = _uiRoot.Find("Meters/Energy/blue");

        _bubbleContainer = _uiRoot.Find("BubbleContainer")?.gameObject;
        _bubbleText = _uiRoot.Find("BubbleContainer/Text (TMP)")?.GetComponent<TMP_Text>();

        if (_bubbleContainer)
            _bubbleContainer.SetActive(false);

        // subscribe לאירועים
        agent.OnPlanChanged += ShowBubble;
        agent.OnActionChanged += ShowBubble; // אם לא רוצים פעולה חדשה – מחק שורה זו
    }

    void LateUpdate()
    {
        if (_uiRoot)
            _uiRoot.position = transform.position + uiOffset;

        UpdateBars();
    }

    void UpdateBars()
    {
        // אצלך זה 0..100 בערך, לפי critical/urgent
        float h = Mathf.Clamp01(needs.hunger / 100f);
        float w = Mathf.Clamp01(needs.warmth / 100f);
        float e = Mathf.Clamp01(needs.energy / 100f);

        SetBar(_hungerBar, h);
        SetBar(_heatBar, w);
        SetBar(_energyBar, e);
    }

    static void SetBar(Transform bar, float t01)
    {
        if (!bar)
            return;
        var s = bar.localScale;
        s.x = Mathf.Max(0f, t01);
        bar.localScale = s;
    }

    void ShowBubble(string goal, string action)
    {
        if (!_bubbleContainer || !_bubbleText)
            return;

        int wood = agent != null ? agent.wood : 0;

        _bubbleText.text = $"Goal: {goal}\n" + $"Action: {action}\n" + $"Wood: {wood}";

        _bubbleContainer.SetActive(true);

        if (_hideCo != null)
            StopCoroutine(_hideCo);
        _hideCo = StartCoroutine(HideAfter(4f));
    }

    System.Collections.IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (_bubbleContainer)
            _bubbleContainer.SetActive(false);
        _hideCo = null;
    }
}
