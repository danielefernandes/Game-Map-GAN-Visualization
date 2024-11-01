using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player; // Referência ao player
    public float mouseSensitivity = 100f;

    private float xRotation = 0f; // Armazena a rotação vertical

    void Start()
    {
        // Trava o cursor no centro da tela e o esconde
        Cursor.lockState = CursorLockMode.Locked;

        // Garante que a rotação inicial da câmera esteja alinhada
        Vector3 initialRotation = transform.localEulerAngles;
        xRotation = initialRotation.x;
    }

    void Update()
    {
        LookAround();
    }

    void LookAround()
    {
        // Leitura do movimento do mouse
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Ajusta a rotação vertical, garantindo que não passe dos limites
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Aplica a rotação vertical à câmera
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotaciona o player horizontalmente
        player.Rotate(Vector3.up * mouseX);
    }
}
