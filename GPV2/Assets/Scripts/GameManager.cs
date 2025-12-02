using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리 필수

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("현재 상태 정보")]
    public Player player;
    public GameObject currentStage;

    void Awake()
    {
        // 1. 싱글톤 + DontDestroyOnLoad 설정
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(scene.name + " 씬이 로드되었습니다. 초기화 시작...");
        StartCoroutine(FindPlayerAndLevel());
    }

    IEnumerator FindPlayerAndLevel()
    {
        yield return null;
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.GetComponent<Player>();
        }

        GameObject foundLevel = GameObject.FindGameObjectWithTag("LevelRoot");

        if (foundLevel != null)
        {
            currentStage = foundLevel;
            Debug.Log($"현재 스테이지 설정 완료: {currentStage.name}");
        }
        else
        {
            Debug.LogWarning("LevelRoot 태그가 달린 오브젝트를 찾지 못했습니다.");
        }
    }
    public void MoveToNextStage(Door transitionDoor)
    {
        if (transitionDoor.nextStage == null)
        {
            return;
        }
        if (currentStage != null)
        {
            currentStage.SetActive(false);
        }
        GameObject newStage = transitionDoor.nextStage;
        newStage.SetActive(true);
        currentStage = newStage;
        RepositionPlayer(transitionDoor.targetEntrance.position);
    }

    void RepositionPlayer(Vector3 targetPosition)
    {
        player.VelocityZero();
        player.transform.position = targetPosition;
    }

}