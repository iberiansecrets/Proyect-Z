using System;
using UnityEngine;

public class ParallaxScript : MonoBehaviour
{
    RectTransform rectTransform;
    RectTransform viewport;
    Vector2 maxOffset;

    [SerializeField] int moveModifier = 10;
    [SerializeField] float smoothness = 2f;

    private bool isEffectActive = true;

    private bool isMobile;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        // Buscar el parallax viewport más cercano
        viewport = GetComponentInParent<ParallaxViewport>().GetComponent<RectTransform>();

        isMobile = Application.isMobilePlatform;

        // Calcular límites correctos por cada parallax
        Vector2 imageSize = GetWorldRect(rectTransform).size;
        Vector2 viewportSize = GetWorldRect(viewport).size;

        float marginX = Mathf.Max(0, (imageSize.x - viewportSize.x) / 2f);
        float marginY = Mathf.Max(0, (imageSize.y - viewportSize.y) / 2f);
        maxOffset = new Vector2(marginX, marginY);

        if (isMobile)
        {
            isEffectActive = false;
        }
    }

    private void Update()
    {
        if (!isEffectActive) return;

        if (isMobile) return;

        Vector2 inputPosition = Input.mousePosition;

        Vector2 pz = Camera.main.ScreenToViewportPoint(inputPosition);

        float targetX = (pz.x - 0.5f) * moveModifier;
        float targetY = (pz.y - 0.5f) * moveModifier;

        targetX = Mathf.Clamp(targetX, -maxOffset.x, maxOffset.x);
        targetY = Mathf.Clamp(targetY, -maxOffset.y, maxOffset.y);

        Vector2 newPos = Vector2.Lerp(rectTransform.anchoredPosition,
                                      new Vector2(targetX, targetY),
                                      smoothness * Time.deltaTime);

        rectTransform.anchoredPosition = newPos;
    }

    public void SetParallaxActive(bool active)
    {
        if (isMobile) return;

        isEffectActive = active;
    }

    Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float width = Vector3.Distance(corners[0], corners[3]);
        float height = Vector3.Distance(corners[0], corners[1]);
        return new Rect(corners[0].x, corners[0].y, width, height);
    }
}
