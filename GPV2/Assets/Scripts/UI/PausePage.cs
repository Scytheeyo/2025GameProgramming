using UnityEngine;
using UnityEngine.SceneManagement;

public class PausePage : MonoBehaviour
{
    [Header("References")]
    public GameObject realOptionCanvas;
    public Player player;
    public Animator uiAnimator; // ★ 인스펙터에 UI_Pause 오브젝트 연결 확인!

    [Header("UI Toggle")]
    public GameObject contentPanel;

    [Header("Confirmation Popup")]
    public GameObject confirmPanel;

    private int pendingAction = 0;
    public SaveManager saveManager;
    private void OnEnable()
    {
        if (player != null) player.UpdateGamePauseState();
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (contentPanel != null) contentPanel.SetActive(true);
        if (saveManager == null)
        {
            saveManager = FindObjectOfType<SaveManager>();
        }
        // ★ [추가] 켜질 때 열리는 애니메이션 발동
        if (uiAnimator != null)
        {
            uiAnimator.SetTrigger("doOpen");
        }
    }

    private void OnDisable()
    {
    }

    // [계속하기 버튼]
    public void ClickResume()
    {
        // ★ [수정] 애니메이터가 있으면 닫는 애니메이션 재생 -> 끝나면 이벤트로 DisableWindow 호출됨
        if (uiAnimator != null)
        {
            uiAnimator.SetTrigger("doClose");
        }
        else
        {
            DisableWindow(); // 애니메이터 없으면 바로 끄기
        }
    }

    // ★ [중요] 애니메이션 클립의 맨 끝 프레임에서 이 함수를 호출해야 함!
    public void DisableWindow()
    {
        gameObject.SetActive(false);
        if (player != null) player.UpdateGamePauseState();
        if (realOptionCanvas != null && realOptionCanvas.activeSelf)
        {
            realOptionCanvas.SetActive(false);
        }
    }

    public void ClickSave()
    {
        if (saveManager != null)
        {
            saveManager.SaveGame(); // SaveManager의 저장 기능 호출

            // (선택사항) 저장이 완료되었다는 알림창이나 로그를 띄울 수 있습니다.
            Debug.Log("게임 저장 명령을 보냈습니다.");
        }
        else
        {
            Debug.LogError("PausePage에 SaveManager가 연결되지 않았습니다!");
            // 비상용: 다시 한 번 찾아보고 실행 시도
            saveManager = FindObjectOfType<SaveManager>();
            if (saveManager != null) saveManager.SaveGame();
        }
    }

    public void ClickOption()
    {
        if (contentPanel != null) contentPanel.SetActive(false);
        realOptionCanvas.SetActive(true);
        gameObject.SetActive(false);
    }

    public void ClickMainMenu() { pendingAction = 1; ShowConfirmPanel(); }
    public void ClickExit() { pendingAction = 2; ShowConfirmPanel(); }
    void ShowConfirmPanel() { if (confirmPanel != null) confirmPanel.SetActive(true); }
    public void OnConfirmPopup()
    {
        Time.timeScale = 1f;
        if (pendingAction == 1) SceneManager.LoadScene("Start");
        else if (pendingAction == 2)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }
        if (confirmPanel != null) confirmPanel.SetActive(false);
        pendingAction = 0;
    }
    public void OnCancelPopup() { if (confirmPanel != null) confirmPanel.SetActive(false); pendingAction = 0; }
}