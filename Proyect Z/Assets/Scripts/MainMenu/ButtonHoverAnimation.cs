using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Animator hoverAnimator;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Pone hover a true y la animación empieza desde 0 siempre
        hoverAnimator.SetBool("hovering", true);
        hoverAnimator.Play("Hover", 0, 0f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Vuelve al estado inicial (Idle)
        hoverAnimator.SetBool("hovering", false);
        hoverAnimator.Play("Idle", 0, 0f);
    }
}

