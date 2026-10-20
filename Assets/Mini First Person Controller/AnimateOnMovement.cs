using UnityEngine;

public class AnimateOnMovement : MonoBehaviour
{
    private Animator animator;
    private float currentSpeed = 0f;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        float targetSpeed = 0f;
        
        // Si hay movimiento en cualquier dirección (W, A, S, D)
        if (moveX != 0 || moveZ != 0)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                targetSpeed = 6f; // Coincide con tu umbral de Run
            }
            else
            {
                targetSpeed = 2f; // Coincide con tu umbral de Walk
            }
        }

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);

        if (animator != null)
        {
            animator.SetFloat("Speed", currentSpeed);
        }
    }
}