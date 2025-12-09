using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Video;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public RawImage gifDisplay;
    public Button leftArrowButton;
    public Button rightArrowButton;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI pageNumberText;

    public VideoPlayer videoPlayer;

    [System.Serializable]
    public class TutorialPage
    {
        public string pageTitle;
        [TextArea]
        public string pageDescription;
        public string videoFileName;
    }

    public List<TutorialPage> tutorialPages;

    private int currentPageIndex = 0;

    void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.isLooping = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        }

        rightArrowButton.onClick.AddListener(NextPage);
        leftArrowButton.onClick.AddListener(PreviousPage);
        UpdatePage(0);
    }

    public void NextPage()
    {
        if (currentPageIndex < tutorialPages.Count - 1)
        {
            currentPageIndex++;
            UpdatePage(currentPageIndex);
        }
    }

    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdatePage(currentPageIndex);
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPrepared;
        vp.Play();
    }

    private void UpdatePage(int newIndex)
    {
        if (newIndex < 0 || newIndex >= tutorialPages.Count)
        {
            Debug.LogError("Índice de página del tutorial fuera de límites: " + newIndex);
            return;
        }

        currentPageIndex = newIndex;
        TutorialPage current = tutorialPages[currentPageIndex];

        int displayedPageNumber = currentPageIndex + 1;

        // 1. Actualizar Textos
        if (titleText != null)
            titleText.text = current.pageTitle;

        if (descriptionText != null)
            descriptionText.text = current.pageDescription;

        // 2. Actualizar GIF
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.prepareCompleted -= OnVideoPrepared;

            if (!string.IsNullOrEmpty(current.videoFileName))
            {
                string path = System.IO.Path.Combine(Application.streamingAssetsPath, current.videoFileName);
                videoPlayer.url = path;

                videoPlayer.prepareCompleted += OnVideoPrepared;
                videoPlayer.Prepare();

                gifDisplay.gameObject.SetActive(true);
            }
            else
            {
                gifDisplay.gameObject.SetActive(false);
            }
        }

        if (pageNumberText != null)
        {
            pageNumberText.text = $"{displayedPageNumber}";
        }

        UpdateArrowButtons();
    }

    private void UpdateArrowButtons()
    {
        int lastPageIndex = tutorialPages.Count - 1;

        leftArrowButton.gameObject.SetActive(currentPageIndex > 0);

        rightArrowButton.gameObject.SetActive(currentPageIndex < lastPageIndex);
    }
}
