using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("위치 설정")]
    public Transform entrancePoint; // 기본 스폰 위치 (중앙)

    [Header("문 설정")]
    // ★ 변경: 입구 역할을 하는 문들을 리스트로 관리 (9번방은 여기에 2개를 넣으세요)
    public List<Door> entranceDoors;

    public List<Door> exitDoors;    // 다음 방으로 가는 문들
}