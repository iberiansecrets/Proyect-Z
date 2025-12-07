using UnityEngine;

public class ParallaxNavigator : MonoBehaviour
{
    public float moveSpeed = 8f;

    private RectTransform rt;
    private Vector2 targetPos;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        targetPos = rt.anchoredPosition; // posición inicial (MainMenu)
    }

    void Update()
    {
        rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPos, Time.deltaTime * moveSpeed);
    }

    public void MoveTo(RectTransform panel)
    {
        // Ponemos la posición del panel como objetivo del ROOT
        targetPos = -panel.anchoredPosition;
    }
}

