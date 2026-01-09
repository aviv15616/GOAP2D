using UnityEngine;

public class Sleep : GoapAction
{
    public float sleepSeconds = 2f;
    public float staminaGainPerSecond = 20f;

    private float t;

    private void Awake()
    {
        // כשעייפים (LowStamina true) רוצים להגיע ל-LowStamina false
        Preconditions["LowStamina"] = true;
        Effects["LowStamina"] = false;
    }

    public override void Perform(GoapAgent agent)
    {
        IsRunning = true;

        t += Time.deltaTime;
        agent.Stamina += staminaGainPerSecond * Time.deltaTime;

        if (t >= sleepSeconds)
        {
            // אנחנו "מסיימים" את הפעולה. ה-Sensor יעדכן LowStamina לפי סטמינה.
            IsRunning = false;
            t = 0f;
        }
    }

    public override void DoReset()
    {
        base.DoReset();
        t = 0f;
    }
}
