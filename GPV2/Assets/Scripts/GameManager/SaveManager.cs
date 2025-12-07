using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement; // 씬 리로드를 위해 필요

public class SaveManager : MonoBehaviour
{
    public Player player;
    public MapGenerator mapGenerator;

    private string saveFileName = "game_save.json";

    public void SaveGame()
    {
        if (player == null || mapGenerator == null) return;

        SaveData data = new SaveData();

        // 1. 맵 데이터 저장 (시드 + 방 번호)
        data.mapSeed = mapGenerator.currentSeed; // 현재 맵의 시드 저장
        data.roomIndex = FindActiveRoomIndex(); // ★ 수정됨: 활성화된 방 찾기

        // 2. 플레이어 데이터 저장
        data.playerPosition = player.transform.position;
        data.currentHealth = player.health;
        data.currentMana = player.mana;

        // 3. 기타 데이터 (카드, 인벤토리 등) 저장 로직은 기존과 동일...
        // (생략: 위에서 작성해드린 코드 그대로 쓰시면 됩니다)

        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        File.WriteAllText(path, json);

        Debug.Log($"저장 완료! 시드: {data.mapSeed}, 방 번호: {data.roomIndex}");
    }

    public void LoadGame()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // ★ 로드 핵심 로직: 
        // 맵 구조를 바꾸기 위해 씬을 다시 시작해야 할 수도 있습니다.
        // 여기서는 "다음 번 씬이 로드될 때 시드를 적용"하고 씬을 리로드하는 방식을 추천합니다.

        MapGenerator.seedToLoad = data.mapSeed; // 1. 시드 예약

        // 2. 씬 리로드 (맵을 처음부터 다시 생성하기 위해)
        // 주의: 씬을 리로드하면 현재 메모리가 날아가므로, 
        // 로드 직후 "플레이어 위치 복구"는 씬이 켜진 뒤(Start)에 처리해야 합니다.
        // 이를 위해 'DontDestroyOnLoad' 객체나 PlayerPrefs 등을 이용해 "로드 모드임"을 알려야 합니다.

        // 간단한 구현을 위해 여기서는 맵 생성기만 리셋 가능하다면 아래처럼 처리합니다.
        // 만약 씬 리로드 없이 즉시 반영하려면 MapGenerator를 수정해서 다시 ConnectMap을 하게 해야 합니다.
        Debug.Log("게임을 로드합니다. (구현 방식에 따라 씬 리로드가 필요할 수 있음)");

        // --- 만약 씬 리로드 없이 즉시 적용한다면 ---
        ApplyLoadData(data);
    }

    // 데이터 실제 적용 함수
    void ApplyLoadData(SaveData data)
    {
        // 1. 플레이어 스탯 복구
        player.health = data.currentHealth;
        player.mana = data.currentMana;

        // 2. 방 활성화 복구
        // 저장된 Index의 방을 찾아서 켜주고, 플레이어 이동
        Room targetRoom = GetRoomByIndex(data.roomIndex);

        if (targetRoom != null)
        {
            // 모든 방 끄기 (MapGenerator에 목록이 있다면 그걸 쓰는게 효율적)
            // 여기서는 BFS로 찾으면서 처리
            DeactivateAllRooms();

            // 목표 방만 켜기
            targetRoom.gameObject.SetActive(true);
            player.transform.position = data.playerPosition;

            // 미니맵 갱신
            MinimapController minimap = FindObjectOfType<MinimapController>();
            if (minimap != null) minimap.OnPlayerEnterRoom(targetRoom);
        }
    }

    // =========================================================
    // ★ 수정된 로직: 활성화(Active)된 방 찾기
    // =========================================================
    private int FindActiveRoomIndex()
    {
        Room startRoom = mapGenerator.room1_Start;
        if (startRoom == null) return 0;

        Queue<Room> queue = new Queue<Room>();
        HashSet<Room> visited = new HashSet<Room>();
        queue.Enqueue(startRoom);
        visited.Add(startRoom);

        int index = 0;

        while (queue.Count > 0)
        {
            Room currentRoom = queue.Dequeue();

            // ★ 거리 체크 삭제 -> "켜져 있니?" 체크로 변경
            if (currentRoom.gameObject.activeSelf)
            {
                return index; // 찾았다!
            }

            foreach (Door door in currentRoom.exitDoors)
            {
                if (door != null && door.nextStage != null)
                {
                    Room nextRoom = door.nextStage.GetComponent<Room>();
                    // 비활성화된 방도 맵 구조상 존재하므로 탐색에는 포함해야 함
                    if (nextRoom != null && !visited.Contains(nextRoom))
                    {
                        visited.Add(nextRoom);
                        queue.Enqueue(nextRoom);
                    }
                }
            }
            index++;
        }
        return 0;
    }

    private void DeactivateAllRooms()
    {
        // BFS를 돌면서 모든 방을 일단 끕니다. (현재 켜진 방을 끄기 위해)
        // MapGenerator에 allRooms 리스트가 있다면 그걸 쓰는게 훨씬 좋습니다.
        // 없다면 BFS로 순회하며 SetActive(false) 실행
        Room startRoom = mapGenerator.room1_Start;
        Queue<Room> queue = new Queue<Room>();
        HashSet<Room> visited = new HashSet<Room>();

        if (startRoom.gameObject.activeSelf) startRoom.gameObject.SetActive(false);

        queue.Enqueue(startRoom);
        visited.Add(startRoom);

        while (queue.Count > 0)
        {
            Room current = queue.Dequeue();
            if (current.gameObject.activeSelf) current.gameObject.SetActive(false);

            foreach (Door door in current.exitDoors)
            {
                if (door && door.nextStage)
                {
                    Room next = door.nextStage.GetComponent<Room>();
                    if (next && !visited.Contains(next))
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                        if (next.gameObject.activeSelf) next.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    // GetRoomByIndex는 이전 코드와 동일 (순서대로 찾아서 반환)
    private Room GetRoomByIndex(int targetIndex)
    {
        Room startRoom = mapGenerator.room1_Start;
        Queue<Room> queue = new Queue<Room>();
        HashSet<Room> visited = new HashSet<Room>();
        queue.Enqueue(startRoom);
        visited.Add(startRoom);

        int index = 0;
        while (queue.Count > 0)
        {
            Room current = queue.Dequeue();
            if (index == targetIndex) return current;

            foreach (Door door in current.exitDoors)
            {
                if (door && door.nextStage)
                {
                    Room next = door.nextStage.GetComponent<Room>();
                    if (next && !visited.Contains(next))
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }
            index++;
        }
        return null;
    }
}