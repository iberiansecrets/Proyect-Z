using UnityEngine;
using UnityEngine.UI;

public class VialChanger : MonoBehaviour
{
    public Image pantallaFin;
    public Sprite victoriaSprite;
    public Sprite derrotaSprite;

    public void MostrarResultado(bool victoria)
    {
        if (pantallaFin == null) return;

        pantallaFin.sprite = victoria ? victoriaSprite : derrotaSprite;
        pantallaFin.gameObject.SetActive(true);
    }
}
