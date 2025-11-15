using UnityEngine;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    [Header("Crosshair UI")]
    public Image crosshairImage;

    [Header("Estado del cursor")]
    public bool useCrosshair = true;

    void Start()
    {
        ActivarCrosshair();
    }

    void Update()
    {
        if (useCrosshair && crosshairImage != null)
        {
            crosshairImage.rectTransform.position = Input.mousePosition;
        }
    }

    public void ActivarCrosshair()
    {
        Cursor.visible = false;
        useCrosshair = true;

        if (crosshairImage != null)
            crosshairImage.enabled = true;
    }

    public void DesactivarCrosshair()
    {
        Cursor.visible = true;
        useCrosshair = false;

        if (crosshairImage != null)
            crosshairImage.enabled = false;
    }
}
