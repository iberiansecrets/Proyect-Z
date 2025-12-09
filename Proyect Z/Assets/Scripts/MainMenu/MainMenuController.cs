using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public ParallaxNavigator navigator;

    public RectTransform mainMenu;
    public RectTransform credits;
    public RectTransform tutorial;
    public GameObject mainMenuObject;
    public GameObject settings;
    public GameObject shop;

    public void Awake()
    {
        mainMenuObject.SetActive(true);
        settings.SetActive(false);
        shop.SetActive(false);
    }

    public void Play()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void GoToSettings()
    {
        if (mainMenuObject.activeSelf)
        {
            mainMenuObject.SetActive(false);
            settings.SetActive(true);
        }
        else
        {
            mainMenuObject.SetActive(true);
            settings.SetActive(false);
        }
    }

    public void GoToShop()
    {
        if (mainMenuObject.activeSelf)
        {
            mainMenuObject.SetActive(false);
            shop.SetActive(true);
        }
        else
        {
            mainMenuObject.SetActive(true);
            shop.SetActive(false);
        }
    }

    public void GoToCredits()
    {
        navigator.MoveTo(credits);
    }

    public void GoToTutorial()
    {
        navigator.MoveTo(tutorial);
    }

    public void GoToMainMenu()
    {
        navigator.MoveTo(mainMenu);
    }
}

