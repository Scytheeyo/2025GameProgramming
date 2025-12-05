using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("위치 설정")]
    public Transform entrancePoint; // 기본 스폰 위치

    [Header("문 설정")]
    public List<Door> entranceDoors;
    public List<Door> exitDoors;    // 다음 방으로 가는 문들

    // ★ 추가: 맵 생성기가 부여할 논리적 ID (1, 2, 3...)
    // 미니맵 데이터 생성 시 식별자로 사용됩니다.
    [HideInInspector] public int roomID = 0;

    // 기존: 플레이어 진입 감지
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            MinimapController minimap = FindObjectOfType<MinimapController>();
            if (minimap != null)
            {
                minimap.OnPlayerEnterRoom(this);
            }
        }
    }
}