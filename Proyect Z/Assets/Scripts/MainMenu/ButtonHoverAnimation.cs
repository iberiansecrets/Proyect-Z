using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverAnimation : MonoBehaviour, IPointerEnterHandler
{
    public Animator animator;

    public void OnPointerEnter(PointerEventData eventData)
    {
        animator.SetTrigger("Hover");
    }
}

