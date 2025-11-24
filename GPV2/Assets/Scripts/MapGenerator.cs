using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("고정 방")]
    public Room room1_Start;
    public Room room9_PreEnd; // ★ 인스펙터에서 EntranceDoors에 문 2개 넣었는지 확인!
    public Room room10_End;

    [Header("랜덤 방 그룹")]
    public List<Room> typeA_Rooms;
    public List<Room> typeB_Rooms;
    public List<Room> typeC_Rooms;

    void Awake()
    {
        ConnectMap();
    }

    void ConnectMap()
    {
        // 1. 초기화 & 셔플 (이전과 동일)
        DeactivateList(typeA_Rooms);
        DeactivateList(typeB_Rooms);
        DeactivateList(typeC_Rooms);
        room9_PreEnd.gameObject.SetActive(false);
        room10_End.gameObject.SetActive(false);
        room1_Start.gameObject.SetActive(true);

        Shuffle(typeA_Rooms);
        Shuffle(typeB_Rooms);
        Shuffle(typeC_Rooms);

        // 2. 역할 배정 (다이아몬드 구조)
        Room splitter = typeA_Rooms[0];

        List<Room> pathPool = new List<Room> { typeA_Rooms[1], typeA_Rooms[2], typeB_Rooms[0], typeB_Rooms[1] };
        Shuffle(pathPool);

        Room left1 = pathPool[0];
        Room left2 = pathPool[1];
        Room right1 = pathPool[2];
        Room right2 = pathPool[3];

        Queue<Room> deadEnds = new Queue<Room>(typeC_Rooms);

        // (1) 1번 -> 분기점 (일반 연결: 입구 인덱스 0)
        LinkTwoWay(room1_Start.exitDoors[0], splitter, 0);

        // (2) 왼쪽 루트
        ConnectComplexStep(splitter.exitDoors[0], left1, deadEnds);
        ConnectComplexStep(GetForwardExit(left1), left2, deadEnds);

        // ★ 왼쪽 루트 끝 -> 9번 방의 [0번 입구]와 연결
        LinkTwoWay(GetForwardExit(left2), room9_PreEnd, 0);


        // (3) 오른쪽 루트
        ConnectComplexStep(splitter.exitDoors[1], right1, deadEnds);
        ConnectComplexStep(GetForwardExit(right1), right2, deadEnds);

        // ★ 오른쪽 루트 끝 -> 9번 방의 [1번 입구]와 연결
        LinkTwoWay(GetForwardExit(right2), room9_PreEnd, 1);


        // (4) 9번 -> 10번 (일방통행, 보스방 입구는 하나라고 가정)
        if (room9_PreEnd.exitDoors.Count > 0)
        {
            Link(room9_PreEnd.exitDoors[0], room10_End);
        }
        else
        {
            Debug.LogError("9번 방(PreEnd)에 출구 문(Exit Door)이 없습니다! 인스펙터를 확인하세요.");
        }


        if (GameManager.instance != null)
        {
            GameManager.instance.currentStage = room1_Start.gameObject;
        }
    }

    // ---------------------------------------------------------
    // 핵심 함수들
    // ---------------------------------------------------------

    void ConnectComplexStep(Door fromDoor, Room targetRoom, Queue<Room> deadEnds)
    {
        // 일반적인 방 연결은 무조건 0번 입구를 사용
        LinkTwoWay(fromDoor, targetRoom, 0);

        if (targetRoom.exitDoors.Count >= 2 && deadEnds.Count > 0)
        {
            Room deadEnd = deadEnds.Dequeue();
            // 막다른 방 연결 (0번 입구 사용)
            LinkTwoWay(targetRoom.exitDoors[1], deadEnd, 0);
        }
    }

    // [업그레이드된 양방향 연결]
    // entranceIndex: 다음 방(nextRoom)의 몇 번째 입구 문과 연결할 것인가?
    void LinkTwoWay(Door outDoor, Room nextRoom, int entranceIndex)
    {
        if (outDoor == null || nextRoom == null) return;

        // 1. 가는 길 (A -> B)
        outDoor.nextStage = nextRoom.gameObject;

        // 중요: 도착 위치를 방 중앙이 아니라, '해당 입구 문 앞'으로 설정하면 더 자연스러움
        // (만약 입구 문이 없다면 그냥 entrancePoint 사용)
        if (nextRoom.entranceDoors.Count > entranceIndex)
        {
            // 그 문 앞으로 이동 (문의 자식으로 SpawnPoint가 있다면 .Find("SpawnPoint") 추천)
            outDoor.targetEntrance = nextRoom.entranceDoors[entranceIndex].transform;
        }
        else
        {
            outDoor.targetEntrance = nextRoom.entrancePoint;
        }

        // 2. 오는 길 (B -> A)
        // nextRoom의 지정된 입구 문(entranceDoors[index])을 가져와서 되돌아가게 설정
        if (nextRoom.entranceDoors.Count > entranceIndex)
        {
            Door targetEntranceDoor = nextRoom.entranceDoors[entranceIndex];

            // 되돌아갈 곳: 출발했던 문(outDoor)이 있는 방
            targetEntranceDoor.nextStage = outDoor.transform.parent.gameObject;

            // 되돌아갈 위치: 출발했던 문(outDoor) 바로 앞
            targetEntranceDoor.targetEntrance = outDoor.transform;
        }
    }

    void Link(Door door, Room nextRoom)
    {
        if (door == null || nextRoom == null) return;

        door.nextStage = nextRoom.gameObject;

        // 만약 nextRoom에 entrancePoint가 할당 안 되어있으면 방 자체의 위치로 이동
        if (nextRoom.entrancePoint != null)
            door.targetEntrance = nextRoom.entrancePoint;
        else
            door.targetEntrance = nextRoom.transform;
    }

    Door GetForwardExit(Room room) { return room.exitDoors[0]; }
    void DeactivateList(List<Room> rooms) { foreach (var r in rooms) if (r) r.gameObject.SetActive(false); }
    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int rand = Random.Range(i, list.Count);
            list[i] = list[rand];
            list[rand] = temp;
        }
    }
}