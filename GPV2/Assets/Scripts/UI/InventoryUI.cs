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
        if (tag == "RedPotion") return "체력 물약";
        if (tag == "BluePotion") return "마나 물약";
        return tag;
    }

    string GetItemDescription(string tag)
    {
        if (tag == "RedPotion") return "체력을 20 회복합니다.\n(더블 클릭하여 사용)";
        if (tag == "BluePotion") return "마나를 20 회복합니다.\n(더블 클릭하여 사용)";
        return "";
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
}