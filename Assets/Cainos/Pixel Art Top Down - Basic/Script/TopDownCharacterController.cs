using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    public class TopDownCharacterController : MonoBehaviour
    {
        public float speed;

        private Animator animator;
        private Rigidbody2D rb;

        private void Start()
        {
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();

            Debug.Log("[DEBUG] Start() — Animator and Rigidbody2D found?");
            Debug.Log("[DEBUG] Animator = " + (animator != null));
            Debug.Log("[DEBUG] Rigidbody2D = " + (rb != null));
        }

        private void Update()
        {
            Vector2 dir = Vector2.zero;

            // LEFT / RIGHT
            if (Input.GetKey(KeyCode.A))
            {
                dir.x = -1;
                animator.SetInteger("Direction", 3);
                Debug.Log("[DEBUG] Pressing A — dir.x = -1");
            }
            else if (Input.GetKey(KeyCode.D))
            {
                dir.x = 1;
                animator.SetInteger("Direction", 2);
                Debug.Log("[DEBUG] Pressing D — dir.x = 1");
            }

            // UP / DOWN
            if (Input.GetKey(KeyCode.W))
            {
                dir.y = 1;
                animator.SetInteger("Direction", 1);
                Debug.Log("[DEBUG] Pressing W — dir.y = 1");
            }
            else if (Input.GetKey(KeyCode.S))
            {
                dir.y = -1;
                animator.SetInteger("Direction", 0);
                Debug.Log("[DEBUG] Pressing S — dir.y = -1");
            }

            // Normalize
            dir.Normalize();

            animator.SetBool("IsMoving", dir.magnitude > 0);

            // Debugging direction info
            Debug.Log("[DEBUG] dir = " + dir + " | magnitude = " + dir.magnitude);

            // Apply velocity
            rb.linearVelocity = speed * dir;

            // Debug velocity
            Debug.Log("[DEBUG] linearVelocity = " + rb.linearVelocity);
        }
    }
}
