using UnityEngine;

public class Door : MonoBehaviour
{
    public GameObject nextStage;
    public Transform targetEntrance;

    public void InitiateTransition()
    {
        // GetComponent<AudioSource>().Play(); 

        // 1. 게임 매니저에게 이동 요청 (플레이어 이동, 방 활성화/비활성화 처리)
        if (GameManager.instance != null)
        {
            GameManager.instance.MoveToNextStage(this);
        }
        else
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다.");
        }

        // =============================================================
        // [추가됨] 미니맵 갱신 요청
        // =============================================================
        UpdateMinimap();
    }

    void UpdateMinimap()
    {
        // 다음 방 정보가 없으면 중단
        if (nextStage == null) return;

        // 다음 방의 Room 컴포넌트 가져오기
        Room nextRoomScript = nextStage.GetComponent<Room>();

        // 미니맵 컨트롤러가 있다면 갱신 함수 호출
        if (MinimapController.instance != null)
        {
            MinimapController.instance.OnPlayerEnterRoom(nextRoomScript);
        }
        else
        {
            // 혹시 싱글톤 인스턴스가 없다면 직접 찾아보기 (안전장치)
            var minimap = FindObjectOfType<MinimapController>();
            if (minimap != null)
            {
                minimap.OnPlayerEnterRoom(nextRoomScript);
            }
        }
    }
}