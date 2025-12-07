using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class MapGenerator : MonoBehaviour
{
    [Header("고정 룸")]
    public Room room1_Start;
    public Room room9_PreEnd;
    public Room room10_End;

    [Header("랜덤 룸 (개수 필수!)")]
    public List<Room> typeA_Rooms; // 3개 (2, 3, 5)
    public List<Room> typeB_Rooms; // 2개 (7, 8)
    public List<Room> typeC_Rooms; // 2개 (4, 6)
    [Header("시드 설정")]
    public int currentSeed; // 현재 맵의 시드 (저장 대상)
    public static int seedToLoad = 0; // 로드할 때 외부에서 값을 넣어주는 변수
    void Start()
    {
        // ★ 1. 시드 결정 로직
        if (seedToLoad != 0)
        {
            currentSeed = seedToLoad; // 로드된 시드 사용
            seedToLoad = 0; // 사용 후 초기화
        }
        else
        {
            currentSeed = Random.Range(0, int.MaxValue); // 새 게임: 랜덤 시드
        }

        // ★ 2. 난수표 고정 (이게 제일 중요!)
        // 이 함수를 호출하면 이후의 Random.Range는 항상 똑같은 순서로 나옵니다.
        Random.InitState(currentSeed);

        if (CheckDependencies()) ConnectMap();
        else Debug.LogError("방 개수 부족!");
    }

    void ConnectMap()
    {
        // 1. 초기화
        DeactivateList(typeA_Rooms);
        DeactivateList(typeB_Rooms);
        DeactivateList(typeC_Rooms);
        room9_PreEnd.gameObject.SetActive(false);
        room10_End.gameObject.SetActive(false);
        room1_Start.gameObject.SetActive(false);

        // 2. 셔플
        Shuffle(typeA_Rooms);
        Shuffle(typeB_Rooms);
        Shuffle(typeC_Rooms);

        // 3. 구조 할당
        Room splitter = typeA_Rooms[0];

        List<Room> leftPath = new List<Room> { typeA_Rooms[1], typeB_Rooms[0] };
        Room leftDeadEnd = typeC_Rooms[0];

        List<Room> rightPath = new List<Room> { typeA_Rooms[2], typeB_Rooms[1] };
        Room rightDeadEnd = typeC_Rooms[1];

        Shuffle(leftPath);
        Shuffle(rightPath);

        // 4. 연결
        // (1) 시작 -> 분기점
        LinkTwoWay(room1_Start.exitDoors[0], splitter, 0);

        // (2) 왼쪽 루트
        ConnectPathSequence(splitter.exitDoors[0], leftPath, leftDeadEnd, room9_PreEnd, 0);

        // (3) 오른쪽 루트
        ConnectPathSequence(splitter.exitDoors[1], rightPath, rightDeadEnd, room9_PreEnd, 1);

        // (4) 보스방 연결
        if (room9_PreEnd.exitDoors.Count > 0)
            Link(room9_PreEnd.exitDoors[0], room10_End);

        // 5. 결과 출력 (수정됨: 모든 경로 출력)
        Debug.Log("<color=cyan>=== [ 맵 생성 완료 : 모든 경로 출력 ] ===</color>");

        // 트리 구조 먼저 출력
        PrintMapHierarchy();
        // ★ 모든 경로(막다른 길 포함) 배열 출력 ★
        PrintAllPaths();

        // 6. 시작
        room1_Start.gameObject.SetActive(true);
        if (GameManager.instance != null) GameManager.instance.currentStage = room1_Start.gameObject;
    }

    // =========================================================
    // [수정됨] 모든 루트를 찾아내는 출력 함수
    // =========================================================
    void PrintAllPaths()
    {
        Debug.Log("<color=yellow>=== [ Detected Routes (Main & Side) ] ===</color>");
        List<string> currentPath = new List<string>();
        FindAndPrintAllPathsRecursive(room1_Start, currentPath);
    }

    void FindAndPrintAllPathsRecursive(Room currentRoom, List<string> path)
    {
        if (currentRoom == null) return;

        path.Add(currentRoom.name);
        bool isLeafNode = true; // 더 이상 갈 곳이 없는 끝인지 체크

        // 1. 보스방 도착 (승리 루트)
        if (currentRoom == room10_End)
        {
            PrintPathLog(path, "<color=green>[MAIN BOSS ROUTE]</color>");
            isLeafNode = false; // 처리했으므로 리프 노드 로직 건너뜀
        }
        else
        {
            // 2. 연결된 문 탐색
            foreach (var door in currentRoom.exitDoors)
            {
                if (door != null && door.nextStage != null)
                {
                    Room nextRoom = door.nextStage.GetComponent<Room>();

                    // 이미 방문한 경로가 아니라면 (무한루프 방지) 탐색 계속
                    if (!path.Contains(nextRoom.name))
                    {
                        isLeafNode = false; // 갈 곳이 있으므로 끝이 아님
                        FindAndPrintAllPathsRecursive(nextRoom, path);
                    }
                }
            }
        }

        // 3. 더 이상 갈 곳이 없는 막다른 방 (사이드 루트)
        // 보스방도 아니고, 갈 수 있는 문도 없다면 여기가 끝입니다.
        if (isLeafNode && currentRoom != room10_End)
        {
            PrintPathLog(path, "<color=orange>[SIDE / DEAD END]</color>");
        }

        // 백트래킹
        path.RemoveAt(path.Count - 1);
    }

    // 경로 예쁘게 출력하는 헬퍼 함수
    void PrintPathLog(List<string> path, string label)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"{label}: ");
        for (int i = 0; i < path.Count; i++)
        {
            sb.Append($"[{path[i]}]");
            if (i < path.Count - 1) sb.Append(" -> ");
        }
        Debug.Log(sb.ToString());
    }

    // ---------------------------------------------------------
    // 기존 유틸리티 함수들 (변경 없음)
    // ---------------------------------------------------------
    void ConnectPathSequence(Door startDoor, List<Room> pathRooms, Room deadEndRoom, Room finalDestination, int finalDestEntranceIndex)
    {
        Door currentExit = startDoor;
        foreach (Room room in pathRooms)
        {
            LinkTwoWay(currentExit, room, 0);
            if (room.exitDoors.Count >= 2 && deadEndRoom != null)
            {
                LinkTwoWay(room.exitDoors[1], deadEndRoom, 0);
                deadEndRoom = null;
            }
            currentExit = room.exitDoors[0];
        }
        LinkTwoWay(currentExit, finalDestination, finalDestEntranceIndex);
    }

    // 필수 함수들 축약 (복사해서 그대로 쓰시면 됩니다)
    bool CheckDependencies() { return room1_Start && room9_PreEnd && room10_End && typeA_Rooms.Count >= 3 && typeB_Rooms.Count >= 2 && typeC_Rooms.Count >= 2; }
    void LinkTwoWay(Door outDoor, Room nextRoom, int entranceIndex)
    {
        if (!outDoor || !nextRoom) return;
        outDoor.nextStage = nextRoom.gameObject;
        if (nextRoom.entranceDoors.Count > entranceIndex) outDoor.targetEntrance = nextRoom.entranceDoors[entranceIndex].transform;
        else if (nextRoom.entrancePoint != null) outDoor.targetEntrance = nextRoom.entrancePoint;
        if (nextRoom.entranceDoors.Count > entranceIndex)
        {
            Door target = nextRoom.entranceDoors[entranceIndex];
            target.nextStage = outDoor.transform.parent.gameObject;
            target.targetEntrance = outDoor.transform;
        }
    }
    void Link(Door door, Room nextRoom)
    {
        if (!door || !nextRoom) return;
        door.nextStage = nextRoom.gameObject;
        if (nextRoom.entrancePoint != null) door.targetEntrance = nextRoom.entrancePoint;
        else door.targetEntrance = nextRoom.transform;
    }

    void PrintMapHierarchy()
    {
        Debug.Log("<color=green>=== [ Map Tree Structure ] ===</color>");
        HashSet<Room> visited = new HashSet<Room>();
        PrintRoomRecursive(room1_Start, "", visited);
    }
    void PrintRoomRecursive(Room currentRoom, string indent, HashSet<Room> visited)
    {
        if (!currentRoom) return;
        if (visited.Contains(currentRoom)) { Debug.Log($"{indent}└─ <color=yellow>[{currentRoom.name}]</color> (Join)"); return; }
        visited.Add(currentRoom);
        string status = currentRoom.gameObject.activeSelf ? "(ON)" : "(OFF)";
        Debug.Log($"{indent}└─ <b>[{currentRoom.name}]</b> {status}");
        foreach (var exit in currentRoom.exitDoors)
        {
            if (exit && exit.nextStage) PrintRoomRecursive(exit.nextStage.GetComponent<Room>(), indent + "    ", visited);
        }
    }

    void DeactivateList(List<Room> rooms) { foreach (var r in rooms) if (r) r.gameObject.SetActive(false); }
    void Shuffle<T>(List<T> list) { for (int i = 0; i < list.Count; i++) { T t = list[i]; int r = Random.Range(i, list.Count); list[i] = list[r]; list[r] = t; } }
}