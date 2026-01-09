using UnityEngine;

public class WorkerAutoTest : MonoBehaviour
{
    public Animator animator;
    public float switchTime = 2f;

    private float timer = 0f;
    private int stateIndex = 0;

    private readonly string[] states = {
        "WorkerWalkDown",
        "WorkerWalkUp",
        "WorkerWalkLeft",
        "WorkerWalkRight"
    };

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        PlayCurrentState();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= switchTime)
        {
            timer = 0f;
            stateIndex = (stateIndex + 1) % states.Length;
            PlayCurrentState();
        }
    }

    private void PlayCurrentState()
    {
        animator.Play(states[stateIndex]);
        Debug.Log("Playing: " + states[stateIndex]);
    }
}
