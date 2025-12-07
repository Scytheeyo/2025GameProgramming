using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카메라 흔들림 및 화면 효과 제어
/// 메인 카메라 혹은 UI 캔버스에 배치된 패널을 제어합니다.
/// </summary>
public class CameraFX : MonoBehaviour
{
    [Header("Shake Settings")]
    public Transform cameraTransform;
    private Vector3 originalPos;

    [Header("Flash/Invert Settings")]
    [Tooltip("화면 전체를 덮는 하얀색/반전색 패널 (UI Image)")]
    public Image flashPanel;
    public Color flashColor = Color.white;
    public Color invertColor = new Color(1f, 0f, 1f, 0.5f); // 마젠타 느낌

    void Awake()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        if (flashPanel != null)
        {
            flashPanel.gameObject.SetActive(false);
            flashPanel.color = Color.clear;
        }
    }

    void OnEnable()
    {
        originalPos = cameraTransform.localPosition;
    }

    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(DoShake(duration, magnitude));
    }

    IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cameraTransform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.localPosition = originalPos;
    }

    // 화면이 번쩍이는 효과 (반전 느낌을 주기 위해 마젠타/보라색 사용 가능)
    public void FlashInvert()
    {
        if (flashPanel == null) return;
        StartCoroutine(DoFlash());
    }

    IEnumerator DoFlash()
    {
        flashPanel.gameObject.SetActive(true);
        flashPanel.color = invertColor; // "공간이 뒤틀린" 색상

        // 순식간에 나타났다가
        yield return new WaitForSeconds(0.05f);

        // 서서히 사라짐
        float t = 1f;
        while (t > 0)
        {
            t -= Time.deltaTime * 5f; // 페이드 아웃 속도
            Color c = invertColor;
            c.a = t;
            flashPanel.color = c;
            yield return null;
        }

        flashPanel.gameObject.SetActive(false);
    }
}