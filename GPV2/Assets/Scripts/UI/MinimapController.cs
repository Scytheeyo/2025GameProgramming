using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MinimapController : MonoBehaviour
{
    public static MinimapController instance;

    [Header("필수 연결")]
    public MapGenerator mapGenerator;
    public GameObject roomUIPrefab;
    public Transform mapContainer;

    [Header("설정")]
    public float gridSpacing = 100f;

    private Dictionary<Room, Vector2Int> roomToCoord = new Dictionary<Room, Vector2Int>();
    private Dictionary<Vector2Int, MinimapRoomUI> coordToUI = new Dictionary<Vector2Int, MinimapRoomUI>();
    private Vector2Int currentPlayerCoord = Vector2Int.zero;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        FindAndInitMapGenerator();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAndInitMapGenerator();
    }

    void FindAndInitMapGenerator()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator>();
        }

        CancelInvoke(nameof(GenerateMinimap));
        Invoke(nameof(GenerateMinimap), 0.2f);
    }

    public void GenerateMinimap()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator>();
        }

        if (mapGenerator == null || mapGenerator.room1_Start == null) return;

        foreach (Transform child in mapContainer) Destroy(child.gameObject);
        roomToCoord.Clear();
        coordToUI.Clear();

        CalculateCoordinates(mapGenerator.room1_Start);

        foreach (var pair in roomToCoord)
        {
            Room room = pair.Key;
            Vector2Int coord = pair.Value;

            GameObject uiObj = Instantiate(roomUIPrefab, mapContainer);
            uiObj.name = $"RoomUI_{coord.x}_{coord.y}";
            uiObj.transform.localPosition = new Vector3(coord.x * gridSpacing, coord.y * gridSpacing, 0);

            MinimapRoomUI uiScript = uiObj.GetComponent<MinimapRoomUI>();
            if (uiScript != null)
            {
                uiScript.InitState();

                if (room == mapGenerator.room10_End) uiScript.SetRoomType(1);
                else if (mapGenerator.typeC_Rooms.Contains(room)) uiScript.SetRoomType(2);
                else uiScript.SetRoomType(0);

                coordToUI.Add(coord, uiScript);
            }
        }

        UpdateBridges();
        OnPlayerEnterRoom(mapGenerator.room1_Start);
    }

    void CalculateCoordinates(Room startRoom)
    {
        Queue<(Room room, Vector2Int pos)> queue = new Queue<(Room, Vector2Int)>();

        roomToCoord.Add(startRoom, Vector2Int.zero);
        queue.Enqueue((startRoom, Vector2Int.zero));

        HashSet<Room> visited = new HashSet<Room> { startRoom };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            Room curRoom = current.room;
            Vector2Int curPos = current.pos;

            for (int i = 0; i < curRoom.exitDoors.Count; i++)
            {
                Door door = curRoom.exitDoors[i];
                if (door == null || door.nextStage == null) continue;

                Room nextRoom = door.nextStage.GetComponent<Room>();
                if (visited.Contains(nextRoom)) continue;

                Vector2Int nextPos = curPos;

                if (nextRoom == mapGenerator.room9_PreEnd)
                {
                    nextPos = new Vector2Int(0, curPos.y + 1);
                }
                else if (mapGenerator.typeA_Rooms.Count > 0 && curRoom == mapGenerator.typeA_Rooms[0])
                {
                    if (i == 0) nextPos = new Vector2Int(curPos.x - 1, curPos.y);
                    else nextPos = new Vector2Int(curPos.x + 1, curPos.y);
                }
                else
                {
                    if (i == 0)
                    {
                        nextPos = new Vector2Int(curPos.x, curPos.y + 1);
                    }
                    else
                    {
                        if (curPos.x < 0) nextPos = new Vector2Int(curPos.x - 1, curPos.y);
                        else nextPos = new Vector2Int(curPos.x + 1, curPos.y);
                    }
                }

                if (!roomToCoord.ContainsKey(nextRoom))
                {
                    roomToCoord.Add(nextRoom, nextPos);
                    visited.Add(nextRoom);
                    queue.Enqueue((nextRoom, nextPos));
                }
            }
        }
    }

    void UpdateBridges()
    {
        foreach (var pair in coordToUI)
        {
            Vector2Int pos = pair.Key;
            MinimapRoomUI ui = pair.Value;
            bool up = coordToUI.ContainsKey(pos + Vector2Int.up);
            bool down = coordToUI.ContainsKey(pos + Vector2Int.down);
            bool left = coordToUI.ContainsKey(pos + Vector2Int.left);
            bool right = coordToUI.ContainsKey(pos + Vector2Int.right);
            ui.SetBridges(up, down, left, right);
        }
    }

    public void OnPlayerEnterRoom(Room room)
    {
        if (room == null || !roomToCoord.ContainsKey(room)) return;

        if (coordToUI.ContainsKey(currentPlayerCoord))
        {
            coordToUI[currentPlayerCoord].SetPlayerIcon(false);
        }

        currentPlayerCoord = roomToCoord[room];
        MinimapRoomUI currentUI = coordToUI[currentPlayerCoord];

        currentUI.SetVisited();
        currentUI.SetPlayerIcon(true);

        RevealNeighbor(currentPlayerCoord + Vector2Int.up);
        RevealNeighbor(currentPlayerCoord + Vector2Int.down);
        RevealNeighbor(currentPlayerCoord + Vector2Int.left);
        RevealNeighbor(currentPlayerCoord + Vector2Int.right);

        if (mapContainer != null)
        {
            mapContainer.localPosition = -new Vector3(currentPlayerCoord.x * gridSpacing, currentPlayerCoord.y * gridSpacing, 0);
        }
    }

    void RevealNeighbor(Vector2Int coord)
    {
        if (coordToUI.ContainsKey(coord))
        {
            coordToUI[coord].SetNeighbor();
        }
    }
}