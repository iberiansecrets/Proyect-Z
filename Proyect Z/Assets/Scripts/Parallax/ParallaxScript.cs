using UnityEngine;
using UnityEngine.UI;

public class ParallaxScript : MonoBehaviour
{
    RectTransform rectTransform;
    Vector2 startPos;
    Vector2 maxOffset;

    [SerializeField] int moveModifier = 10;
    [SerializeField] float smoothness = 2f;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        startPos = Vector2.zero;

        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();

        Vector2 imageSize = GetWorldRect(rectTransform).size;
        Vector2 canvasSize = GetWorldRect(canvasRect).size;

        float marginX = Mathf.Max(0, (imageSize.x - canvasSize.x) / 2f);
        float marginY = Mathf.Max(0, (imageSize.y - canvasSize.y) / 2f);
        maxOffset = new Vector2(marginX, marginY);
    }

    private void Update()
    {
        Vector2 pz = Camera.main.ScreenToViewportPoint(Input.mousePosition);

        float targetX = (pz.x - 0.5f) * moveModifier;
        float targetY = (pz.y - 0.5f) * moveModifier;

        targetX = Mathf.Clamp(targetX, -maxOffset.x, maxOffset.x);
        targetY = Mathf.Clamp(targetY, -maxOffset.y, maxOffset.y);

        Vector2 newPos = Vector2.Lerp(rectTransform.anchoredPosition, new Vector2(targetX, targetY), smoothness * Time.deltaTime);
        rectTransform.anchoredPosition = newPos;
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
