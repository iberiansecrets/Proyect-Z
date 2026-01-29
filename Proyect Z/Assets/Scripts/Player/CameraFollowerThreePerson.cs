using UnityEngine;

public class CameraFollowerThreePerson : MonoBehaviour
{
    public Transform targetA;
    public Transform targetB;

    // Un buen offset para "detrás-arriba" sería algo como (0, 5, -10)
    public Vector3 offset = new Vector3(0f, 5f, -7f);
    public float suavizado = 5f; // Para que el movimiento no sea brusco

    private Transform currentTarget;

    // Se usa LateUpdate para cámaras para asegurar que el player ya se movió este frame
    void LateUpdate()
    {
        // 1. Detectar cuál está activo
        if (targetA != null && targetA.gameObject.activeSelf)
            currentTarget = targetA;
        else if (targetB != null && targetB.gameObject.activeSelf)
            currentTarget = targetB;

        if (currentTarget != null)
        {
            // 2. Calcular la posición deseada teniendo en cuenta la rotación del target
            // TransformPoint convierte el offset local (detrás del player) a una posición en el mundo
            Vector3 posicionDeseada = currentTarget.TransformPoint(offset);

            // 3. Mover la cámara suavemente (opcional, puedes usar asignación directa)
            transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavizado * Time.deltaTime);

            // 4. Hacer que la cámara mire siempre al jugador
            transform.LookAt(currentTarget.position + Vector3.up * 1.5f); // Apuntamos un poco arriba del pie
        }
    }
}
