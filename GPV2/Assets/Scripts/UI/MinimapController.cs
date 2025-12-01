using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapController : MonoBehaviour
{
    [Header("Settings")]
    public GameObject roomPrefab;       // 위에서 만든 RoomCell 프리팹
    public Transform mapContainer;      // 시계 안의 MinimapContainer
    public float cellDistance = 35f;    // 방과 방 사이의 간격 (프리팹 크기 + 여백)

    [Header("Icons")]
    public Sprite playerIconSprite;     // (선택) 플레이어 아이콘

    // 생성된 미니맵 UI들을 저장할 딕셔너리 (좌표 : UI오브젝트)
    private Dictionary<Vector2Int, MinimapRoomUI> generatedRooms = new Dictionary<Vector2Int, MinimapRoomUI>();
    private Vector2Int currentPos;

    // 초기화: 맵 데이터를 받아서 UI를 쫙 깔아주는 함수
    // MapData는 실제 게임의 맵 생성 로직에서 쓰는 데이터 구조체여야 합니다.
    public void InitializeMinimap(List<Vector2Int> allRoomCoords)
    {
        // 기존 맵 삭제
        foreach (Transform child in mapContainer) Destroy(child.gameObject);
        generatedRooms.Clear();

        // 맵 생성
        foreach (Vector2Int coord in allRoomCoords)
        {
            GameObject newRoom = Instantiate(roomPrefab, mapContainer);

            // 위치 잡기 (0,0을 기준으로 간격만큼 벌림)
            newRoom.transform.localPosition = new Vector3(coord.x * cellDistance, coord.y * cellDistance, 0);

            MinimapRoomUI uiScript = newRoom.GetComponent<MinimapRoomUI>();
            if (uiScript == null) uiScript = newRoom.AddComponent<MinimapRoomUI>(); // 스크립트가 없다면 붙여줌

            generatedRooms.Add(coord, uiScript);

            // 처음엔 안개(Fog)에 가려져 안보이게 처리하려면 여기서 SetActive(false) 혹은 투명도 조절
            uiScript.SetVisiblity(false);
        }
    }

    // 플레이어가 방을 이동했을 때 호출
    public void UpdatePlayerPosition(Vector2Int newCoord)
    {
        // 1. 이전 위치의 플레이어 마커 끄기
        if (generatedRooms.ContainsKey(currentPos))
        {
            generatedRooms[currentPos].SetPlayerIcon(false);
        }

        // 2. 새 위치 갱신
        currentPos = newCoord;

        // 3. 새 위치의 플레이어 마커 켜기 & 방 밝히기 (Visited 처리)
        if (generatedRooms.ContainsKey(currentPos))
        {
            generatedRooms[currentPos].SetPlayerIcon(true);
            generatedRooms[currentPos].SetVisiblity(true);

            // (옵션) 인접한 방도 살짝 보여주기 (아이작 스타일)
            RevealNeighbor(currentPos + Vector2Int.up);
            RevealNeighbor(currentPos + Vector2Int.down);
            RevealNeighbor(currentPos + Vector2Int.left);
            RevealNeighbor(currentPos + Vector2Int.right);
        }

        // 4. (중요) 미니맵 전체를 이동시켜서 현재 플레이어가 시계 중앙에 오도록 함
        mapContainer.localPosition = new Vector3(-currentPos.x * cellDistance, -currentPos.y * cellDistance, 0);
    }

    void RevealNeighbor(Vector2Int coord)
    {
        if (generatedRooms.ContainsKey(coord))
        {
            generatedRooms[coord].SetVisiblity(true); // 혹은 "가봤던 방"과는 다르게 희미하게 표시
        }
    }
}