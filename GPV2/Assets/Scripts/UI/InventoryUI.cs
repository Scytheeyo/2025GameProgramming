using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public Player player;
    public GameObject slotPrefab;

    [Header("Slot Parent")]
    public Transform scrollContent;

    [Header("Equip Slot References")]
    public Image weaponSlotImage;

    [Header("Left Panel UI (Details)")]
    public Image selectedItemIcon;
    public TextMeshProUGUI selectedItemName;
    public TextMeshProUGUI selectedItemDesc;

    [Header("Left Panel UI (Effects)")]
    public TextMeshProUGUI activeCardsEffectText;
    public TextMeshProUGUI activeDeckEffectText;

    [Header("Animation")]
    public Animator uiAnimator; // ★ 인스펙터에서 자기 자신(InventoryWindow)을 연결하세요!

    private List<GameObject> allSlots = new List<GameObject>();
    private int totalSlots = 30; // 전체 슬롯 개수

    // 더블 클릭 감지용
    private float lastClickTime = 0f;
    private string lastClickedTag = "";
    private float doubleClickThreshold = 0.3f;

    void Awake()
    {
        // 1. 슬롯 초기화 (기존 슬롯 삭제 후 재생성)
        foreach (Transform child in scrollContent) Destroy(child.gameObject);
        allSlots.Clear();

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slot = Instantiate(slotPrefab, scrollContent);
            slot.name = $"Slot_{i}";
            allSlots.Add(slot);
        }

        ClearDetailPanel();
    }

    void OnEnable()
    {
        // 인벤토리 창이 열릴 때마다 화면을 새로 고침
        RefreshInventoryUI();
        RefreshEffectTexts();
        RefreshEquippedWeaponUI();
    }

    // ★★★ [핵심] 아이템을 슬롯에 그리는 함수 ★★★
    public void RefreshInventoryUI()
    {
        if (player == null) return;

        // 플레이어의 인벤토리 목록(키 값들)을 가져옴
        List<string> itemTags = player.inventory.Keys.ToList();

        for (int i = 0; i < allSlots.Count; i++)
        {
            GameObject slot = allSlots[i];

            // 슬롯 내부 컴포넌트 찾기
            Button slotButton = slot.GetComponent<Button>();
            Image itemIcon = slot.transform.Find("ItemIcon").GetComponent<Image>();
            TextMeshProUGUI itemCount = slot.transform.Find("ItemCount").GetComponent<TextMeshProUGUI>();

            // 버튼 기능 초기화
            slotButton.onClick.RemoveAllListeners();

            // 인벤토리에 아이템이 있는 인덱스인지 확인
            if (i < itemTags.Count)
            {
                // [아이템이 있는 슬롯 처리]
                string tag = itemTags[i];            // 아이템 이름 (예: RedPotion)
                int count = player.inventory[tag];   // 아이템 개수

                // 플레이어가 기억해둔 스프라이트(그림) 가져오기
                Sprite iconSprite = null;
                if (player.knownItemSprites.ContainsKey(tag))
                {
                    iconSprite = player.knownItemSprites[tag];
                }

                // 1. 아이콘 설정
                itemIcon.sprite = iconSprite;
                itemIcon.enabled = (iconSprite != null); // 그림이 없으면 하얀 네모가 뜨니까 숨김 처리

                // 2. 개수 설정 (1개일 때는 숫자 안 보이게)
                if (count > 1)
                {
                    itemCount.text = count.ToString();
                    itemCount.gameObject.SetActive(true);
                }
                else
                {
                    itemCount.text = "";
                    itemCount.gameObject.SetActive(false);
                }

                // 3. 버튼 활성화
                slotButton.interactable = true;
                slotButton.onClick.AddListener(() => OnSlotClicked(tag, iconSprite));
            }
            else
            {
                // [빈 슬롯 처리]
                itemIcon.sprite = null;
                itemIcon.enabled = false; // ★ 아이콘 이미지 끄기 (중요)
                itemCount.text = "";
                itemCount.gameObject.SetActive(false);

                slotButton.interactable = false; // 빈 슬롯은 클릭 불가
            }
        }
    }

    void OnSlotClicked(string itemTag, Sprite icon)
    {
        // 정보창 표시
        selectedItemIcon.sprite = icon;
        selectedItemIcon.enabled = (icon != null);
        selectedItemName.text = GetPrettyName(itemTag);
        selectedItemDesc.text = GetItemDescription(itemTag);

        // 더블 클릭 (사용) 로직
        if (lastClickedTag == itemTag && (Time.time - lastClickTime) < doubleClickThreshold)
        {
            player.UseItem(itemTag);
            RefreshInventoryUI(); // 사용 후 개수 갱신

            if (!player.inventory.ContainsKey(itemTag))
                ClearDetailPanel();

            lastClickTime = 0f;
            lastClickedTag = "";
        }
        else
        {
            lastClickedTag = itemTag;
            lastClickTime = Time.time;
        }
    }

    // 텍스트 갱신
    void RefreshEffectTexts()
    {
        if (player == null) return;
        UpdatePassiveStats();
        UpdateActiveDeckStats();
    }

    // ★ [수정됨] 형식: 원래 스탯 (+추가치) : 총합
    void UpdatePassiveStats()
    {
        if (activeCardsEffectText == null || player == null) return;

        List<CardData> allCards = player.collectedCards;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // 1. 카드 보너스 합계 계산
        int sumSpade = allCards.Where(c => c.suit == CardSuit.Spade).Sum(c => c.number);
        int sumHeart = allCards.Where(c => c.suit == CardSuit.Heart).Sum(c => c.number);
        int sumDiamond = allCards.Where(c => c.suit == CardSuit.Diamond).Sum(c => c.number);
        int sumClover = allCards.Where(c => c.suit == CardSuit.Clover).Sum(c => c.number);

        // 2. 텍스트 구성
        // 공격력 (Spade)
        sb.Append($"♠ 공격력 {player.baseAttackDamage} <color=#FF5555>(+{sumSpade})</color> : {player.currentAttackDamage}");
        sb.AppendLine();

        // 체력 (Heart)
        sb.Append($"♥ 체력 {player.baseMaxHealth} <color=#FF5555>(+{sumHeart})</color> : {player.maxHealth}");
        sb.AppendLine();

        // 방어력 (Diamond)
        sb.Append($"♦ 방어력 {player.baseDefense} <color=#FF5555>(+{sumDiamond})</color> : {player.currentDefense}");
        sb.AppendLine();

        // 마나 (Clover)
        sb.Append($"♣ 마나 {player.baseMaxMana} <color=#FF5555>(+{sumClover})</color> : {player.maxMana}");
        sb.AppendLine();

        if (allCards.Count == 0)
        {
            // 카드가 없어도 기본 스탯은 보여주도록 유지 (원한다면 "카드 없음" 텍스트로 대체 가능)
            // sb.Clear(); 
            // sb.Append("보유 카드 없음");
        }

        activeCardsEffectText.text = sb.ToString();
    }

    void UpdateActiveDeckStats()
    {
        if (activeDeckEffectText == null) return;
        List<CardData> deck = player.activeDeck;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (deck.Count < 5) sb.Append($"덱 완성 필요 ({deck.Count}/5)");
        else
        {
            // 족보 로직
            var numberGroups = deck.GroupBy(c => c.number).ToList();
            var suitGroups = deck.GroupBy(c => c.suit).ToList();
            var sortedNumbers = deck.Select(c => c.number).Distinct().OrderBy(n => n).ToList();

            bool isFlush = suitGroups.Any(g => g.Count() == 5);
            bool isStraight = (sortedNumbers.Count == 5) && (sortedNumbers.Last() - sortedNumbers.First() == 4);
            bool isFourCard = numberGroups.Any(g => g.Count() == 4);
            bool isFullHouse = numberGroups.Any(g => g.Count() == 3) && numberGroups.Any(g => g.Count() == 2);
            bool isTriple = numberGroups.Any(g => g.Count() == 3);
            bool isPair = numberGroups.Any(g => g.Count() == 2);

            if (isStraight && isFlush) sb.Append("스트레이트 플러시\n(적 즉시 처치)");
            else if (isFourCard) sb.Append("포카드\n(올스탯 10% 상승)");
            else if (isFullHouse) sb.Append("풀하우스\n(추가 타격)");
            else if (isFlush) sb.Append("플러시\n(쿨타임 감소)");
            else if (isStraight) sb.Append("스트레이트\n(1회 부활)");
            else if (isTriple) sb.Append("트리플\n(더블 점프 강화)");
            else if (isPair) sb.Append("페어\n(더블 점프 가능)");
            else sb.Append("노 페어\n(효과 없음)");
        }
        activeDeckEffectText.text = sb.ToString();
    }

    void ClearDetailPanel()
    {
        if (selectedItemIcon) selectedItemIcon.enabled = false;
        if (selectedItemName) selectedItemName.text = "";
        if (selectedItemDesc) selectedItemDesc.text = "아이템을 선택하세요.";
    }

    string GetPrettyName(string tag)
    {
        if (tag == "RedPotion") return "치유의 물약";
        if (tag == "BluePotion") return "마나의 물약";

        if (tag == "normalSword") return "수습 기사의 철검";

        if (tag == "Wand5") return "<color=#AA88FF>대마법사의 지팡이</color>";

        if (tag == "LegendSword") return "<color=#FFBB00>고대 영웅의 검</color>";

        return tag;
    }

    string GetItemDescription(string tag)
    {

        if (tag == "RedPotion")
            return "붉은 활력이 담긴 물약입니다. 마시면 상처가 아물고 기운이 솟아납니다.\n\n" +
                   "<color=#FF5555>♥ 체력 20 회복</color>\n" +
                   "<size=80%><color=#AAAAAA>(더블 클릭하여 사용)</color></size>";

        if (tag == "BluePotion")
            return "응축된 마력이 담긴 신비한 물약입니다. 정신을 맑게 해줍니다.\n\n" +
                   "<color=#5555FF>♣ 마나 20 회복</color>\n" +
                   "<size=80%><color=#AAAAAA>(더블 클릭하여 사용)</color></size>";

        if (tag == "normalSword")
            return "왕국 병사들에게 지급되는 표준 검입니다. 날이 잘 서 있어 다루기 쉽습니다.\n\n" +
                   "Type: <color=white>근접 무기</color>\n" +
                   "<size=80%><color=#FFFF55>(더블 클릭하여 장착)</color></size>";

        if (tag == "Wand5")
            return "강력한 마력이 깃든 나무로 깎았습니다. 지팡이 끝에서 마력을 방출합니다.\n\n" +
                   "Type: <color=#AA88FF>원거리 무기 (마법)</color>\n" +
                   "<size=80%><color=#FFFF55>(더블 클릭하여 장착)</color></size>";

        if (tag == "LegendSword")
            return "전설 속의 영웅이 사용했다는 검입니다. 뿜어져 나오는 압도적인 기운이 적을 제압합니다.\n\n" +
                   "Type: <color=#FFBB00>근접 무기 (전설)</color>\n" +
                   "<size=80%><color=#FFFF55>(더블 클릭하여 장착)</color></size>";

        return "알 수 없는 아이템입니다.";
    }

    public void CloseWindow()
    {
        if (uiAnimator != null)
        {
            uiAnimator.SetTrigger("doClose");
        }
        else
        {
            DisableWindow();
        }
    }

    public void DisableWindow()
    {
        gameObject.SetActive(false);
        if (player != null) player.UpdateGamePauseState();
    }
    public void RefreshEquippedWeaponUI()
    {
        if (player == null || weaponSlotImage == null) return;

        // 플레이어가 무기를 들고 있는지 확인
        if (player.equippedWeapon != null)
        {
            // 1. 무기 이름 가져오기 ((Clone) 제거)
            string weaponName = player.equippedWeapon.gameObject.name.Replace("(Clone)", "").Trim();

            // 2. 이미 알고 있는 스프라이트인지 확인 후 적용
            if (player.knownItemSprites.ContainsKey(weaponName))
            {
                weaponSlotImage.sprite = player.knownItemSprites[weaponName];

                // 색상을 하얗게(투명도 없음) 설정 (중요: 빈 슬롯일 때 투명하게 했다면 다시 켜야 함)
                weaponSlotImage.color = Color.white;
                weaponSlotImage.enabled = true;
            }
            else
            {
                // 스프라이트 정보가 없으면, 현재 들고 있는 무기 오브젝트에서 직접 가져오기
                SpriteRenderer sr = player.equippedWeapon.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    weaponSlotImage.sprite = sr.sprite;
                    weaponSlotImage.color = Color.white;
                    weaponSlotImage.enabled = true;
                }
            }
        }
        else
        {
            // 장착된 무기가 없으면?
            // 방법 A: 이미지를 끈다 (아예 안 보임)
            // weaponSlotImage.enabled = false; 

            // 방법 B: 투명하게 만든다 (배경 틀은 보이고 아이콘만 숨김) - 추천
            weaponSlotImage.sprite = null;
            Color c = weaponSlotImage.color;
            c.a = 0f; // 완전 투명
            weaponSlotImage.color = c;
        }
    }
}