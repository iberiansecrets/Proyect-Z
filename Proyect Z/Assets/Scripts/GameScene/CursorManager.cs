using UnityEngine;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    [Header("Crosshair UI")]
    public Image crosshairImage;

    [Header("Estado del cursor")]
    public bool useCrosshair = true;

    [SerializeField]private bool isMobile;

    void Start()
    {
        isMobile = Application.isMobilePlatform;

        // En móvil nunca va a estar el crosshair personalizado
        if (isMobile) 
        {
            DesactivarCrosshair();
        }
        else
        {
            ActivarCrosshair();
        }
    }

    void Update()
    {

        // Se mueve SOLAMENTE si no estamos en móvil
        if (!isMobile && useCrosshair && crosshairImage != null)
        {
            crosshairImage.rectTransform.position = Input.mousePosition;
        }
    }

    public void ActivarCrosshair()
    {
        // Nunca activar en móvil
        if (isMobile) return;

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
