using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayButtonAnimation : MonoBehaviour
{
    public Image playButtonImage;       // Imagen del botón
    public Sprite normalSprite;         // Sprite normal
    public Sprite pressedSprite;        // Sprite al pulsar
    public float delay = 1f;            // Tiempo de espera antes de continuar

    public void OnPlayPressed()
    {
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        // Cambiar sprite
        if (playButtonImage != null && pressedSprite != null)
            playButtonImage.sprite = pressedSprite;

        // Esperar
        yield return new WaitForSeconds(delay);

        // Cargar escena
        SceneManager.LoadScene("GameScene");
    }
}
