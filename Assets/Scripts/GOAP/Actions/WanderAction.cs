using UnityEngine;

public class WanderAction : GoapAction
{
    [Header("Wander")]
    public float wanderRadius = 6f;
    public float arriveDistance = 0.2f;
    public float waitAtPointSeconds = 0.6f;

    [Header("Map Bounds (2D)")]
    public Vector2 minBounds = new Vector2(-10f, -10f);
    public Vector2 maxBounds = new Vector2(10f, 10f);
    public float boundsPadding = 0.15f; // מרווח קטן מהקיר

    [Header("Stuck Detection")]
    public float stuckCheckEvery = 0.15f;
    public float minProgressPerCheck = 0.02f; // כמה חייב להתקדם בין בדיקות
    public int maxStuckStrikes = 3;           // כמה פעמים רצוף לפני החלפת יעד

    private Vector3 _target;
    private float _waitT;

    // runtime for stuck detection
    private float _stuckT;
    private Vector2 _lastPos;
    private int _stuckStrikes;

    public override bool CanPlan(WorldState s) => true;

    public override void ApplyPlanEffects(GoapAgent agent, ref WorldState s)
    {
        // תכנון: לא "קופצים" לנקודה מחוץ למפה
        // אם אתה רוצה לדמות תזוזה בתכנון - תצמיד לגבולות
        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        Vector2 p = s.pos + offset;
        s.pos = ClampToBounds(p);
    }

    public override bool StartAction(GoapAgent agent)
    {
        _waitT = 0f;
        _stuckT = 0f;
        _stuckStrikes = 0;
        _lastPos = agent ? (Vector2)agent.transform.position : Vector2.zero;

        PickNewTarget(agent);
        return true;
    }

    public override bool Perform(GoapAgent agent, float dt)
    {
        if (!EnsureStarted(agent))
            return true;

        // אם אנחנו ממש צמודים לגבול, עדיף לבחור יעד חדש פנימה (מונע "דחיפה" על הקיר)
        if (IsNearBounds(agent.transform.position))
        {
            PickNewTarget(agent);
        }

        bool arrived = agent.mover.MoveTowards(_target, dt, arriveDistance);
        if (!arrived)
        {
            // בדיקת תקיעות: אם כמעט לא זזנו זמן קצר ברצף -> יעד חדש
            _stuckT += dt;
            if (_stuckT >= stuckCheckEvery)
            {
                Vector2 cur = agent.transform.position;
                float moved = Vector2.Distance(cur, _lastPos);

                if (moved < minProgressPerCheck)
                    _stuckStrikes++;
                else
                    _stuckStrikes = 0;

                _lastPos = cur;
                _stuckT = 0f;

                if (_stuckStrikes >= maxStuckStrikes)
                {
                    PickNewTarget(agent);
                    _stuckStrikes = 0;
                }
            }

            return false;
        }

        // הגעה ליעד -> המתנה קצרה ואז סיום
        _waitT += dt;
        return _waitT >= waitAtPointSeconds;
    }

    private void PickNewTarget(GoapAgent agent)
    {
        Vector2 basePos = agent ? (Vector2)agent.transform.position : Vector2.zero;
        basePos = ClampToBounds(basePos);

        // נסיונות כדי למצוא נקודה בתוך המפה וגם בתוך הרדיוס
        for (int i = 0; i < 12; i++)
        {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector2 candidate = basePos + offset;

            candidate = ClampToBounds(candidate);

            // לוודא שעדיין לא יצא לנו משהו “תקוע” ממש על הקיר
            if (!IsNearBounds(candidate))
            {
                _target = candidate;
                return;
            }
        }

        // fallback: פשוט בחר נקודה פנימית אקראית בגבולות
        _target = new Vector2(
            Random.Range(minBounds.x + boundsPadding, maxBounds.x - boundsPadding),
            Random.Range(minBounds.y + boundsPadding, maxBounds.y - boundsPadding)
        );
    }

    private Vector2 ClampToBounds(Vector2 p)
    {
        return new Vector2(
            Mathf.Clamp(p.x, minBounds.x + boundsPadding, maxBounds.x - boundsPadding),
            Mathf.Clamp(p.y, minBounds.y + boundsPadding, maxBounds.y - boundsPadding)
        );
    }

    private bool IsNearBounds(Vector2 p)
    {
        return (p.x <= minBounds.x + boundsPadding) ||
               (p.x >= maxBounds.x - boundsPadding) ||
               (p.y <= minBounds.y + boundsPadding) ||
               (p.y >= maxBounds.y - boundsPadding);
    }

    public override void ResetRuntime()
    {
        base.ResetRuntime();
        _waitT = 0f;
        _target = Vector3.zero;
        _stuckT = 0f;
        _stuckStrikes = 0;
        _lastPos = Vector2.zero;
    }

    public override void ApplyPlanEffects(ref WorldState s)
    {
        // Wander בדרך כלל בלי אפקט קבוע
    }
}
