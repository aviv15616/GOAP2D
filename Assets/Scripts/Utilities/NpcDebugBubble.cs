//// Assets/Scripts/UI/NpcDebugBubble.cs
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class NpcDebugBubble : MonoBehaviour
//{
//    public Transform followTarget;
//    public Vector3 offset = new Vector3(0, 1.6f, 0);

//    [Header("Text")]
//    public TMP_Text goalText;
//    public TMP_Text actionText;
//    public TMP_Text planText;

//    [Header("Bars")]
//    public Image staminaFill;
//    public Image hungerFill;
//    public Image warmthFill;

//    private GoapAgent _agent;
//    private Needs _needs;

//    public void Bind(GoapAgent agent)
//    {
//        _agent = agent;
//        _needs = agent.needs;

//        agent.OnReplanned += _ => RefreshTexts();
//        agent.OnGoalChanged += _ => RefreshTexts();
//        agent.OnActionChanged += _ => RefreshTexts();

//        RefreshTexts();
//    }

//    private void LateUpdate()
//    {
//        if (followTarget)
//            transform.position = followTarget.position + offset;

//        RefreshBars();
//    }

//    private void RefreshTexts()
//    {
//        if (!_agent) return;
//        if (goalText) goalText.text = _agent.Debug_CurrentGoal ?? "-";
//        if (actionText) actionText.text = _agent.Debug_CurrentAction ?? "-";
//        if (planText) planText.text = _agent.Debug_PlanStack ?? "(empty)";
//    }

//    private void RefreshBars()
//    {
//        if (!_needs) return;

//        // Replace these with your real fields (0..1)
//        if (staminaFill) staminaFill.fillAmount = Mathf.Clamp01(_needs.stamina01);
//        if (hungerFill) hungerFill.fillAmount = Mathf.Clamp01(_needs.hunger01);
//        if (warmthFill) warmthFill.fillAmount = Mathf.Clamp01(_needs.warmth01);
//    }
//}
