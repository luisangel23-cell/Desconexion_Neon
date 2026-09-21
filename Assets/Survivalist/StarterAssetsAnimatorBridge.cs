using UnityEngine;

public class StarterAssetsAnimatorBridge : MonoBehaviour
{
    private CharacterController capsuleController;
    private Animator animator;

    void Start()
    {
        // Busca el control físico en la cápsula de arriba
        capsuleController = GetComponentInParent<CharacterController>();
        // Obtiene el Animator del propio Survivalist
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (capsuleController != null && animator != null)
        {
            // Calcula la velocidad horizontal de la cápsula
            float speed = new Vector3(capsuleController.velocity.x, 0, capsuleController.velocity.z).magnitude;
            
            // Le manda la velocidad al Blend Tree del Animator
            animator.SetFloat("Speed", speed);
            
            // Asegura que esté tocando el suelo para que no se quede en bucle de salto
            animator.SetBool("Grounded", capsuleController.isGrounded);
        }
    }
}