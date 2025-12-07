using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class CardListUI : MonoBehaviour
{
    [Header("References")]
    public Player player;
    public Transform collectionContent; // 보유 카드 슬롯 부모
    public Transform deckContent;       // 장착 덱 슬롯 부모
    public GameObject cardPrefab;       // 카드 프리팹
    public Sprite[] allCardSprites;     // 카드 이미지 배열

    [Header("Display Texts")]
    public TextMeshProUGUI cardDescriptionText; // 보유 카드 전체 능력치 합계 표시 (패시브)
    public TextMeshProUGUI deckCombinationText; // 현재 덱의 족보 효과 표시 (액티브)

    [Header("Animation")]
    public Animator uiAnimator; // ★ 인스펙터에서 자기 자신(CardListWindow)을 연결하세요!

    void OnEnable()
    {
        RefreshBothPanels();
        UpdateUI_Texts(); // 창이 열릴 때 텍스트 갱신
    }

    // 통합 갱신 함수
    void UpdateUI_Texts()
    {
        RefreshTotalPassiveStats(); // 왼쪽 텍스트: 전체 스탯 합계
        RefreshActiveDeckHand();    // 오른쪽 텍스트: 족보 효과
    }

    // 컬렉션과 덱 패널 UI 갱신
    void RefreshBothPanels()
    {
        if (player == null || collectionContent == null || deckContent == null || cardPrefab == null)
        {
            Debug.LogError("CardListUI에 필요한 참조가 연결되지 않았습니다.");
            return;
        }

        // 1. 컬렉션(보유 카드) 패널 새로 고침
        foreach (Transform child in collectionContent)
        {
            Destroy(child.gameObject);
        }

        List<CardSuit> suitOrder = new List<CardSuit> { CardSuit.Spade, CardSuit.Heart, CardSuit.Diamond, CardSuit.Clover };

        foreach (CardSuit suit in suitOrder)
        {
            for (int number = 1; number <= 13; number++)
            {
                CardData slotCardRef = new CardData(suit, number);
                int cardCount = player.collectedCards.Count(c => c.suit == suit && c.number == number);
                CardData firstInstance = player.collectedCards.FirstOrDefault(c => c.suit == suit && c.number == number);
                bool isInDeck = (firstInstance != null) && player.activeDeck.Contains(firstInstance);

                // --- UI 오브젝트 생성 ---
                GameObject cardObj = Instantiate(cardPrefab, collectionContent);
                Image cardImage = cardObj.GetComponent<Image>();
                Button cardButton = cardObj.GetComponent<Button>();

                // 스프라이트 설정
                Sprite cardSprite = GetCardSprite(slotCardRef);
                if (cardImage != null && cardSprite != null) cardImage.sprite = cardSprite;

                // 수량 텍스트 설정
                Transform countTextTrans = cardObj.transform.Find("CountText");
                if (countTextTrans != null)
                {
                    TextMeshProUGUI countText = countTextTrans.GetComponent<TextMeshProUGUI>();
                    if (countText != null)
                    {
                        countText.text = "" + cardCount;
                        countText.gameObject.SetActive(cardCount > 1);
                    }
                }

                // 버튼 초기화
                cardButton.onClick.RemoveAllListeners();

                // 덱 이동 기능
                if (cardCount > 0 && !isInDeck)
                {
                    cardImage.color = Color.white;
                    cardButton.interactable = true;
                    cardButton.onClick.AddListener(() => MoveCardToDeck(firstInstance));
                }
                else
                {
                    cardImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                    cardButton.interactable = false;
                }
            }
        }

        // 2. 덱(활성 덱) 패널 새로 고침
        foreach (Transform child in deckContent)
        {
            Destroy(child.gameObject);
        }

        List<CardData> sortedDeck = player.activeDeck.OrderBy(c => c.number).ThenBy(c => c.suit).ToList();

        foreach (CardData cardInDeck in sortedDeck)
        {
            CreateDeckCardUI(cardInDeck);
        }
    }

    void CreateDeckCardUI(CardData card)
    {
        GameObject cardObj = Instantiate(cardPrefab, deckContent);
        Sprite cardSprite = GetCardSprite(card);
        Image cardImage = cardObj.GetComponent<Image>();

        if (cardImage != null && cardSprite != null) cardImage.sprite = cardSprite;

        Transform countTextTrans = cardObj.transform.Find("CountText");
        if (countTextTrans != null) countTextTrans.gameObject.SetActive(false);

        Button cardButton = cardObj.GetComponent<Button>();
        if (cardButton)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => MoveCardToCollection(card));
        }
    }

    public void MoveCardToDeck(CardData card)
    {
        if (player.activeDeck.Count >= 5) return;
        if (!player.activeDeck.Contains(card))
        {
            player.activeDeck.Add(card);
        }

        RefreshBothPanels();
        UpdateUI_Texts();
    }

    public void MoveCardToCollection(CardData card)
    {
        if (player.activeDeck.Contains(card))
        {
            player.activeDeck.Remove(card);
        }

        RefreshBothPanels();
        UpdateUI_Texts();
    }

    // ================================================================
    // 1. [상시 출력] 보유 카드 전체 능력치 합계
    // ================================================================
    public void RefreshTotalPassiveStats()
    {
        if (cardDescriptionText == null || player == null) return;

        List<CardData> allCards = player.collectedCards;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (allCards.Count == 0)
        {
            sb.AppendLine("보유 카드 없음");
        }
        else
        {
            int sumSpade = allCards.Where(c => c.suit == CardSuit.Spade).Sum(c => c.number);
            int sumHeart = allCards.Where(c => c.suit == CardSuit.Heart).Sum(c => c.number);
            int sumDiamond = allCards.Where(c => c.suit == CardSuit.Diamond).Sum(c => c.number);
            int sumClover = allCards.Where(c => c.suit == CardSuit.Clover).Sum(c => c.number);

            if (sumSpade > 0) sb.AppendLine($"♠ 공격력 <color=#FF5555>+{sumSpade * 1}</color>");
            if (sumHeart > 0) sb.AppendLine($"♥ 체력 <color=#FF5555>+{sumHeart * 1}</color>");
            if (sumDiamond > 0) sb.AppendLine($"♦ 방어력 <color=#FF5555>+{sumDiamond * 1}</color>");
            if (sumClover > 0) sb.AppendLine($"♣ 마나 <color=#FF5555>+{sumClover * 1}</color>");
        }

        cardDescriptionText.text = sb.ToString();
    }

    // ================================================================
    // 2. [상시 출력] 현재 덱 족보 효과
    // ================================================================
    public void RefreshActiveDeckHand()
    {
        if (deckCombinationText == null || player == null) return;

        List<CardData> deck = player.activeDeck;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (deck.Count < 5)
        {
            sb.AppendLine($"<color=#AAAAAA>카드 부족 ({deck.Count}/5)</color>");
        }
        else
        {
            var numberGroups = deck.GroupBy(c => c.number).ToList();
            var suitGroups = deck.GroupBy(c => c.suit).ToList();
            var sortedNumbers = deck.Select(c => c.number).Distinct().OrderBy(n => n).ToList();

            bool isFourCard = numberGroups.Any(g => g.Count() == 4);
            bool isFullHouse = numberGroups.Any(g => g.Count() == 3) && numberGroups.Any(g => g.Count() == 2);
            bool isTriple = numberGroups.Any(g => g.Count() == 3);
            bool isPair = numberGroups.Any(g => g.Count() == 2);
            bool isStraight = (sortedNumbers.Count == 5) && (sortedNumbers.Last() - sortedNumbers.First() == 4);
            bool isFlush = suitGroups.Any(g => g.Count() == 5);
            bool isStraightFlush = isStraight && isFlush;

            if (isStraightFlush)
            {
                sb.AppendLine("<color=#FF00FF>* 스트레이트 플러시 *</color>");
                sb.AppendLine("궁극기: 모든 적 즉시 처치");
            }
            else if (isFourCard)
            {
                sb.AppendLine("<color=#FF00FF>* 포카드 (Four of a Kind) *</color>");
                sb.AppendLine("모든 능력치 10% 상승");
            }
            else if (isFullHouse)
            {
                sb.AppendLine("<color=#00FFFF>* 풀하우스 (Full House) *</color>");
                sb.AppendLine("공격 시 추가 타격 발동");
            }
            else if (isFlush)
            {
                sb.AppendLine("<color=#00FFFF>* 플러시 (Flush) *</color>");
                sb.AppendLine("스킬 쿨타임 20% 감소");
            }
            else if (isStraight)
            {
                sb.AppendLine("<color=#FFFF00>* 스트레이트 (Straight) *</color>");
                sb.AppendLine("사망 시 1회 부활");
            }
            else if (isTriple)
            {
                sb.AppendLine("<color=#FFA500>* 트리플 (Three of a Kind) *</color>");
                sb.AppendLine("더블 점프 강화");
            }
            else if (isPair)
            {
                int pairCount = numberGroups.Count(g => g.Count() == 2);
                string title = pairCount >= 2 ? "투 페어" : "원 페어";
                sb.AppendLine($"<color=#FFA500>* {title} *</color>");
                sb.AppendLine("더블 점프 가능");
            }
            else
            {
                sb.AppendLine("노 페어 (High Card)");
            }
        }

        deckCombinationText.text = sb.ToString();
    }

    private Sprite GetCardSprite(CardData card)
    {
        int numberIndex = card.number - 1;
        int finalIndex = 0;

        if (numberIndex < 0 || numberIndex > 12) return null;

        switch (card.suit)
        {
            case CardSuit.Heart: finalIndex = 0 + numberIndex; break;
            case CardSuit.Diamond: finalIndex = 13 + numberIndex; break;
            case CardSuit.Spade: finalIndex = 26 + numberIndex; break;
            case CardSuit.Clover: finalIndex = 39 + numberIndex; break;
        }

        if (allCardSprites.Length > finalIndex) return allCardSprites[finalIndex];
        return null;
    }

    // ★ [수정됨] 닫기 버튼용 함수 (애니메이션 트리거 실행)
    public void CloseWindow()
    {
        if (uiAnimator != null)
        {
            uiAnimator.SetTrigger("doClose");
        }
        else
        {
            DisableWindow(); // 애니메이터가 없으면 바로 끔
        }
    }

    // ★ [추가됨] 애니메이션 이벤트용 함수 (진짜 끄기)
    public void DisableWindow()
    {
        gameObject.SetActive(false);
        if (player != null) player.UpdateGamePauseState();
    }
}