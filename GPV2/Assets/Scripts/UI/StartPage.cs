using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class StartPage : MonoBehaviour
{
    [Header("Canvas Objects")]
    public GameObject startCanvas;
    public GameObject startPannel;
    public GameObject title;
    public GameObject optionCanvas;
    public Animator uiAnimator;

    [Header("Scene Settings")]
    public string gameSceneName;

    [Header("Fade Settings")]
    public Image fadePanel;
    public float fadeDuration = 1.0f;


    public void OpenOption()
    {
        startPannel.SetActive(false);
        title.SetActive(false);
        optionCanvas.SetActive(true);
    }

    public void CloseOption()
    {
        optionCanvas.SetActive(false);
        startPannel.SetActive(true);
        title.SetActive(true);
        uiAnimator.SetTrigger("doOpen");
    }

    public void LoadGameScene()
    {
        StartCoroutine(FadeOutAndLoad());
    }

    IEnumerator FadeOutAndLoad()
    {
        fadePanel.gameObject.SetActive(true);
        uiAnimator.SetTrigger("doClose");   

        float timer = 0f;
        Color color = fadePanel.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadePanel.color = color;
            yield return null;
        }

        color.a = 1f;
        fadePanel.color = color;

        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}