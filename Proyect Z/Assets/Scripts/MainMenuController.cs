using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject settings;
    public GameObject shop;
    public GameObject credits;
    public GameObject customization;
    
    public void Play()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void ToggleSettings()
    {
        if (mainMenu.activeSelf)
        {
            mainMenu.SetActive(false);
            settings.SetActive(true);
        }
        else
        {
            mainMenu.SetActive(true);
            settings.SetActive(false);
        }
    }

    public void ToggleShop()
    {
        if (mainMenu.activeSelf)
        {
            mainMenu.SetActive(false);
            shop.SetActive(true);
        }
        else
        {
            mainMenu.SetActive(true);
            shop.SetActive(false);
        }
    }

    public void ToggleCredits()
    {
        if (mainMenu.activeSelf)
        {
            mainMenu.SetActive(false);
            credits.SetActive(true);
        }
        else
        {
            mainMenu.SetActive(true);
            credits.SetActive(false);
        }
    }    
    
    public void ToggleCustomization()
    {
        if (mainMenu.activeSelf)
        {
            mainMenu.SetActive(false);
            customization.SetActive(true);
        }
        else
        {
            mainMenu.SetActive(true);
            customization.SetActive(false);
        }
    }
}
