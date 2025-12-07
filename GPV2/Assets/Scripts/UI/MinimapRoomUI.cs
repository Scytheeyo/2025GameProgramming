using UnityEngine;
using UnityEngine.UI;

public class MinimapRoomUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image baseSlot;           // BaseSlot (배경)
    public GameObject currentMarker; // CurrentMarker (현재 위치)

    [Header("Icons Group")]
    public GameObject iconsRoot;     // Icons (부모)
    public GameObject bossIcon;      // Icons 하위 -> BossIcon
    public GameObject potionIcon;    // Icons 하위 -> PotionIcon

    [Header("Bridges Group")]
    public GameObject bridgesRoot;   // Bridges (부모)
    public GameObject[] bridges;     // Bridges 하위 -> U, D, L, R 순서

    private bool isVisited = false;

    // 초기화
    public void InitState()
    {
        isVisited = false;
        gameObject.SetActive(false); // 전체 숨김 (BaseSlot 포함)

        // 1. 마커 숨김
        if (currentMarker) currentMarker.SetActive(false);

        // 2. 아이콘 그룹 숨김 및 내부 아이콘 초기화
        if (iconsRoot) iconsRoot.SetActive(false);
        if (bossIcon) bossIcon.SetActive(false);     // ★ 중요: 일단 다 끔
        if (potionIcon) potionIcon.SetActive(false); // ★ 중요: 일단 다 끔

        // 3. 다리 그룹 숨김
        if (bridgesRoot) bridgesRoot.SetActive(false);
    }

    // ★ [추가됨] 방의 종류(보스방, 아이템방 등)를 설정하는 함수
    // 0: 일반, 1: 보스, 2: 아이템(포션)
    public void SetRoomType(int type)
    {
        // 아이콘을 미리 켜두지 않고, 어떤 걸 켤지만 준비 상태로 둠
        // 실제 노출은 SetVisited()에서 iconsRoot를 켤 때 이루어짐

        if (type == 1 && bossIcon != null)
        {
            bossIcon.SetActive(true);
            if (potionIcon) potionIcon.SetActive(false);
        }
        else if (type == 2 && potionIcon != null)
        {
            potionIcon.SetActive(true);
            if (bossIcon) bossIcon.SetActive(false);
        }
        else
        {
            // 일반 방이면 다 끔
            if (bossIcon) bossIcon.SetActive(false);
            if (potionIcon) potionIcon.SetActive(false);
        }
    }

    // 상태 1: 방문함 (밝게 + 아이콘 그룹 표시)
    public void SetVisited()
    {
        isVisited = true;
        gameObject.SetActive(true);

        // ★ 방문했을 때만 아이콘 그룹(부모)을 켬
        // (SetRoomType에서 미리 켜둔 자식 아이콘이 이때 같이 보임)
        if (iconsRoot) iconsRoot.SetActive(true);

        if (baseSlot) baseSlot.color = Color.white;
    }

    // 상태 2: 이웃함 (어둡게 + 아이콘 그룹 숨김)
    public void SetNeighbor()
    {
        if (isVisited) return;

        gameObject.SetActive(true);

        // 이웃 상태에서는 아이콘 그룹을 꺼서 뭔지 모르게 함
        if (iconsRoot) iconsRoot.SetActive(false);

        if (baseSlot) baseSlot.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        if (currentMarker) currentMarker.SetActive(false);
    }

    public void SetPlayerIcon(bool isActive)
    {
        if (currentMarker) currentMarker.SetActive(isActive);
    }

    public void SetBridges(bool up, bool down, bool left, bool right)
    {
        // ★ 중요: 부모(Bridges)를 먼저 켜줘야 자식이 보임
        if (bridgesRoot) bridgesRoot.SetActive(true);

        if (bridges != null && bridges.Length >= 4)
        {
            if (bridges[0]) bridges[0].SetActive(up);
            if (bridges[1]) bridges[1].SetActive(down);
            if (bridges[2]) bridges[2].SetActive(left);
            if (bridges[3]) bridges[3].SetActive(right);
        }
    }
}