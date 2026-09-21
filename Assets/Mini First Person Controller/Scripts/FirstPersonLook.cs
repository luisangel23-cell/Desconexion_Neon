using UnityEngine;

public class FirstPersonLook : MonoBehaviour
{
    public float sensitivity = 2f;
    public float smoothing = 1.5f;

    private float rotationX = 0f;
    private Vector2 frameVelocity;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);

        // 1. La cámara rota arriba y abajo de forma local (limitada a -90 y 90)
        rotationX -= frameVelocity.y;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

        // 2. Rota de lado a lado automáticamente al objeto padre (el personaje)
        if (transform.parent != null)
        {
            transform.parent.Rotate(Vector3.up * frameVelocity.x);
        }
    }
}