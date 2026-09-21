using UnityEngine;

public class RigidbodyAnimatorBridge : MonoBehaviour
{
    private Rigidbody rb;
    private Animator animator;
    private bool isGrounded = true;

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (rb != null && animator != null)
        {
            // 1. Velocidad horizontal para el caminar/correr
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            float speed = horizontalVel.magnitude;
            float finalSpeed = speed > 0.1f ? Mathf.Max(speed, 2.0f) : 0f;

            if (HasParameter("Speed"))
                animator.SetFloat("Speed", finalSpeed, 0.05f, Time.deltaTime);

            if (HasParameter("MotionSpeed"))
                animator.SetFloat("MotionSpeed", 1f);

            // 2. Detección segura del suelo mediante Raycast hacia abajo
            isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f);
            
            if (HasParameter("Grounded"))
                animator.SetBool("Grounded", isGrounded);

            // 3. Disparador de salto limpio al presionar Espacio
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                if (HasParameter("Jump"))
                {
                    animator.SetTrigger("Jump");
                }
            }
        }
    }

    bool HasParameter(string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}