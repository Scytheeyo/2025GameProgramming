using System.Collections;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;

public class SceneFadeManager : MonoBehaviour
{

    public static SceneFadeManager Instance { get; private set; }

    public Image fadeImage; 
    public float fadeDuration = 1.0f; 

    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(FadeAndLoadScene(sceneName));
    }

    public void QuitGameWithFade()
    {
        StartCoroutine(FadeOutAndQuit());
    }
    public void NextLevelWithFade()
    {
        StartCoroutine(FadeInOut());
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        yield return StartCoroutine(Fade(0, 1)); // 알파 0 -> 1
        SceneManager.LoadScene(sceneName);
        yield return StartCoroutine(Fade(1, 0)); // 알파 1 -> 0
    }
    private IEnumerator FadeInOut()
    {
        yield return StartCoroutine(Fade(0, 1)); // 알파 0 -> 1
        yield return StartCoroutine(Fade(1, 0)); // 알파 1 -> 0
    }

    private IEnumerator FadeOutAndQuit()
    {
        yield return StartCoroutine(Fade(0, 1)); 
        Debug.Log("게임 종료!");
        Application.Quit();
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsedTime = 0.0f;
        Color color = fadeImage.color;
        fadeImage.gameObject.SetActive(true);

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeDuration);
            fadeImage.color = new Color(color.r, color.g, color.b, newAlpha);
            yield return null; 
        }

        fadeImage.color = new Color(color.r, color.g, color.b, endAlpha);
        if (endAlpha == 0)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }
}