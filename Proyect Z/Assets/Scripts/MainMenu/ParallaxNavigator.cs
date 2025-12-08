using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ParallaxNavigator : MonoBehaviour
{
    public float moveSpeed = 8f;

    private RectTransform rt;
    private Vector2 targetPos;
    private bool isMoving = false;

    private List<ParallaxScript> allParallaxScripts;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        targetPos = rt.anchoredPosition;

        allParallaxScripts = new List<ParallaxScript>(GetComponentsInChildren<ParallaxScript>(true));
    }

    void Update()
    {
        if (isMoving) return;

        rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPos, Time.deltaTime * moveSpeed);
    }

    public void MoveTo(RectTransform panel)
    {
        Vector2 newTargetPos = -panel.anchoredPosition;

        if (newTargetPos != targetPos && !isMoving)
        {
            targetPos = newTargetPos;
            StartCoroutine(Navigate(targetPos));
        }
    }

    private IEnumerator Navigate(Vector2 finalPosition)
    {
        isMoving = true;

        SetAllParallaxActive(false);

        Vector2 startPos = rt.anchoredPosition;
        float timeElapsed = 0f;
        float duration = 1f / moveSpeed;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / duration;

            t = Mathf.SmoothStep(0f, 1f, t);

            rt.anchoredPosition = Vector2.Lerp(startPos, finalPosition, t);
            yield return null;
        }

        rt.anchoredPosition = finalPosition;

        SetAllParallaxActive(true);

        isMoving = false;
    }

    private void SetAllParallaxActive(bool active)
    {
        foreach (ParallaxScript script in allParallaxScripts)
        {
            script.SetParallaxActive(active);
        }
    }
}

