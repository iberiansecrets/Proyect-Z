using UnityEngine;

public class Rotator : MonoBehaviour
{
    [Header("Velocidad de rotación (grados por segundo)")]
    public float rotationSpeed = 90f;

    [Header("Dirección de rotación")]
    public bool rotateClockwise = true;

    private float baseSpeed;
    private static float multiplier = 1f;
    private static bool sliderActive = false;
    private int click = 0;
    private float currentSpeed;

    void Awake()
    {
        baseSpeed = rotationSpeed;
    }

    void Update()
    {
        if (click < 4)
        {
            currentSpeed = rotationSpeed;
        } 
        else
        {
            currentSpeed = sliderActive ? baseSpeed * multiplier : rotationSpeed;
        }
        float direction = rotateClockwise ? -1f : 1f;
        float angle = currentSpeed * Time.deltaTime * direction;

        if (TryGetComponent<RectTransform>(out RectTransform rt))
        {
            rt.Rotate(0f, 0f, angle);
        }
        else
        {
            transform.Rotate(0f, 0f, angle);
        }
    }

    public void OnSliderValueChanged(float value)
    {
        multiplier = value;
        sliderActive = true;
        click = click + 1;
    }

    public void OnSliderEndDrag()
    {
        sliderActive = false;
    }
}
