using UnityEngine;
using UnityEngine.UI;

public class CursorMainMenu : MonoBehaviour
{
    [Header("Cursor textures")]
    public Sprite idleCursor;
    public Sprite handCursor;

    [Header("Imagen UI del cursor")]
    public Image cursorImage;

    [Header("Configuración")]
    public bool useCursor = true;

    [Header("Cursor hotspot")]
    public Vector2 hotspot = Vector2.zero;

    [Header("Control de Plataforma")]
    public bool forceMobileInEditor = false; // Para pruebas en el Editor
    private bool isMobile;


    void Awake()
    {
        #if UNITY_EDITOR
            isMobile = forceMobileInEditor;
        #else
            isMobile = Application.isMobilePlatform;
        #endif

        if (isMobile)
        {
            useCursor = false;
            if (cursorImage != null)
                cursorImage.enabled = false;

            Cursor.visible = true;
            return;
        }


        if (!useCursor || cursorImage == null) return;

        Cursor.visible = false;

        SetIdleCursor();
    }

    void Update()
    {
        if (!useCursor || cursorImage == null) return;

        // Mueve el cursor UI a la posición del mouse
        cursorImage.rectTransform.position = Input.mousePosition;
    }

    // Llamar cuando quieras que el cursor sea normal
    public void SetIdleCursor()
    {
        if (cursorImage != null && idleCursor != null)
            cursorImage.sprite = idleCursor;
    }

    // Llamar cuando quieres que sea mano
    public void SetHandCursor()
    {
        if (cursorImage != null && handCursor != null)
            cursorImage.sprite = handCursor;
    }

    // Opcional: activar/desactivar cursor
    public void SetCursorEnabled(bool enabled)
    {
        if (isMobile) return;

        useCursor = enabled;

        if (cursorImage != null)
            cursorImage.enabled = enabled;

        Cursor.visible = !enabled; // Si desactivamos, mostramos cursor del sistema
    }
}

