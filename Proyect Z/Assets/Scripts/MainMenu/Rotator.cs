using UnityEngine;

public class Rotator : MonoBehaviour
{
    [Header("Velocidad de rotación (grados por segundo)")]
    public float rotationSpeed = 90f;

    [Header("Dirección de rotación")]
    public bool rotateClockwise = true;

    void Update()
    {
        float direction = rotateClockwise ? -1f : 1f;
        float angle = rotationSpeed * Time.deltaTime * direction;

        // Si es UI (RectTransform)
        if (TryGetComponent<RectTransform>(out RectTransform rt))
        {
            rt.Rotate(0f, 0f, angle);
        }
        else
        {
            // Si es un objeto 3D/normal
            transform.Rotate(0f, 0f, angle);
        }
    }
}
