using UnityEngine;
using UnityEngine.UI;

public class MinimapRoomUI : MonoBehaviour
{
    public Image baseSlot;
    public GameObject currentMarker; // 플레이어 위치 표시 (노란 테두리/앨리스)
    public GameObject[] bridges;     // 0:Up, 1:Down, 2:Left, 3:Right 순서라고 가정
    public GameObject iconGroup;     // 보스/포션 아이콘 부모

    public void SetPlayerIcon(bool isActive)
    {
        if (currentMarker != null) currentMarker.SetActive(isActive);
    }

    public void SetVisiblity(bool isVisible)
    {
        // 안보일 때는 아예 끄거나, 색을 검게 하거나
        gameObject.SetActive(isVisible);
    }

    // 맵 생성 시 연결 정보를 받아 통로를 뚫어주는 함수
    public void SetBridges(bool up, bool down, bool left, bool right)
    {
        if (bridges.Length >= 4)
        {
            bridges[0].SetActive(up);
            bridges[1].SetActive(down);
            bridges[2].SetActive(left);
            bridges[3].SetActive(right);
        }
    }
}