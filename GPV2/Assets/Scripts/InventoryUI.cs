using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

// ▼▼▼ ItemSpriteLookup 헬퍼 클래스 *삭제됨* ▼▼▼
// [System.Serializable] public class ItemSpriteLookup { ... }

public class InventoryUI : MonoBehaviour
{
    // [유니티 인스펙터에서 연결]
    public Player player;            // (필수)
    public Transform slotGridContent; // (필수) 'SlotContainer' 오브젝트
    public GameObject slotPrefab;      // (필수) 'SlotPrefab'

    // ▼▼▼ ItemSpriteDatabase 리스트 *삭제됨* ▼▼▼
    // public List<ItemSpriteLookup> itemSpriteDatabase;

    private List<GameObject> createdSlots = new List<GameObject>();

    // ▼▼▼ [수정된 부분] ▼▼▼
    private int maxSlots = 20; // 30에서 20으로 변경
    // ▲▲▲ [여기까지] ▲▲▲

    void Awake()
    {
        // 1. 20개의 빈 슬롯을 미리 생성
        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slot = Instantiate(slotPrefab, slotGridContent);
            slot.name = "InventorySlot_" + i;

            slot.GetComponent<Button>().interactable = false;
            slot.transform.Find("ItemIcon").GetComponent<Image>().enabled = false;
            slot.transform.Find("ItemCount").GetComponent<TextMeshProUGUI>().text = "";

            createdSlots.Add(slot);
        }
    }

    void OnEnable()
    {
        RefreshInventoryUI();
    }

    public void RefreshInventoryUI()
    {
        if (player == null) return;

        List<string> itemTags = player.inventory.Keys.ToList();

        for (int i = 0; i < createdSlots.Count; i++)
        {
            GameObject slot = createdSlots[i];
            Button slotButton = slot.GetComponent<Button>();
            Image itemIcon = slot.transform.Find("ItemIcon").GetComponent<Image>();
            TextMeshProUGUI itemCount = slot.transform.Find("ItemCount").GetComponent<TextMeshProUGUI>();

            slotButton.onClick.RemoveAllListeners();

            if (i < itemTags.Count)
            {
                // [아이템이 있는 슬롯]
                string currentTag = itemTags[i];
                int currentCount = player.inventory[currentTag];

                // ▼▼▼ [스프라이트 가져오는 로직 *변경*] ▼▼▼
                // DB 대신 Player의 딕셔너리에서 스프라이트를 찾음
                Sprite currentSprite = null;
                if (player.knownItemSprites.ContainsKey(currentTag))
                {
                    currentSprite = player.knownItemSprites[currentTag];
                }
                // ▲▲▲ [여기까지] ▲▲▲

                // 4-1. 아이콘과 개수 표시
                itemIcon.sprite = currentSprite;
                itemIcon.enabled = (currentSprite != null); // ★
                itemCount.text = currentCount.ToString();

                // 4-2. 버튼 활성화 및 클릭 이벤트 연결
                slotButton.interactable = true;
                slotButton.onClick.AddListener(() => UseItem(currentTag));
            }
            else
            {
                // [빈 슬롯]
                itemIcon.sprite = null;
                itemIcon.enabled = false; // ★
                itemCount.text = "";
                slotButton.interactable = false;
            }
        }
    }

    private void UseItem(string itemTagToUse)
    {
        player.UseItem(itemTagToUse);
        RefreshInventoryUI();
    }
}