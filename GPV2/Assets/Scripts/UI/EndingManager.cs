using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class EndingManager : MonoBehaviour
{
    [Header("Scene Names")]
    // ★ 게임 플레이 씬 이름 (Stage1)
    public string gameSceneName = "Stage1";

    // ★ 메인 메뉴 씬 이름 (Start)
    public string titleSceneName = "Start";

    [Header("Settings")]
    private string saveFileName = "game_save.json";
    private int defaultHealth = 100;
    private int defaultMana = 100;

    // ========================================================================
    // 1. [게임 종료] 버튼
    // ========================================================================
    public void OnClick_QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        Debug.Log("게임 종료");
    }

    // ========================================================================
    // 2. [다음 게임 시작] 버튼 (카드 유지 + Stage1 로드)
    // ========================================================================
    public void OnClick_NewGamePlus()
    {
        // 다음 회차 데이터 저장
        SaveForNextRun();

        // Stage1 씬으로 이동
        SceneManager.LoadScene(gameSceneName);
    }

    // ========================================================================
    // 3. [메인 메뉴로] 버튼 (카드 유지 + Start 로드)
    // ========================================================================
    public void OnClick_GoToMainMenu()
    {
        // 다음 회차 데이터 저장
        SaveForNextRun();

        // Start 씬으로 이동
        SceneManager.LoadScene(titleSceneName);
    }

    // ========================================================================
    // [내부 로직] 다음 회차를 위한 데이터 리셋 및 저장
    // ========================================================================
    private void SaveForNextRun()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // --- New Game+ 초기화 로직 ---

            // 1. 위치 및 스탯 초기화
            data.currentHealth = defaultHealth;
            data.currentMana = defaultMana;
            data.inventoryItems.Clear(); // 인벤토리 비우기
            data.roomIndex = 0;          // 0번 방부터 시작

            // 2. 맵 시드 초기화 (0이면 랜덤 생성)
            data.mapSeed = 0;
            data.playerPosition = Vector3.zero;

            // 3. ★ 핵심: collectedCards(카드 목록)는 건드리지 않음 (계승)

            // 4. 저장
            string newJson = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, newJson);

            Debug.Log("데이터 리셋 및 카드 계승 완료.");
        }
    }
}