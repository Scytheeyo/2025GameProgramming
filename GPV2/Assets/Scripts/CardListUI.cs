using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;


public class CardListUI : MonoBehaviour
{
    public Player player;

    public Transform collectionContent;
    public Transform deckContent;
    
    public GameObject cardPrefab;
    public Sprite[] allCardSprites;

    void OnEnable()
    {
        RefreshBothPanels();
    }

    void RefreshBothPanels()
    {
        if (player == null || collectionContent == null || deckContent == null || cardPrefab == null)
        {
            Debug.LogError("CardListUI에 필요한 참조가 연결되지 않았습니다.");
            return;
        }

        // ▼▼▼ [수정된 부분] ▼▼▼
        // 1. 컬렉션(보유 카드) 패널 새로 고침 (52개 고정 슬롯)
        foreach (Transform child in collectionContent)
        {
            Destroy(child.gameObject);
        }

        // GetCardSprite의 스프라이트 순서(하트, 다이아, 스페이드, 클로버)를 따릅니다.
        List<CardSuit> suitOrder = new List<CardSuit> { CardSuit.Spade, CardSuit.Heart, CardSuit.Diamond, CardSuit.Clover };

        foreach (CardSuit suit in suitOrder)
        {
            for (int number = 1; number <= 13; number++)
            {
                // 이 슬롯에 해당하는 카드 데이터 생성 (참조용)
                CardData slotCardData = new CardData(suit, number);

                // 이 카드를 플레이어가 보유했는지 확인 (실제 인스턴스)
                CardData ownedCardInstance = player.collectedCards.FirstOrDefault(c => c.suit == suit && c.number == number);

                // 보유한 카드가 덱에 있는지 확인
                bool isInDeck = (ownedCardInstance != null) && player.activeDeck.Contains(ownedCardInstance);

                // --- UI 오브젝트 생성 및 설정 ---
                GameObject cardObj = Instantiate(cardPrefab, collectionContent);
                Image cardImage = cardObj.GetComponent<Image>();
                Button cardButton = cardObj.GetComponent<Button>();

                Sprite cardSprite = GetCardSprite(slotCardData); // 스프라이트 가져오기

                if (cardImage != null && cardSprite != null)
                {
                    cardImage.sprite = cardSprite;
                }

                cardButton.onClick.RemoveAllListeners();

                // --- 상태에 따라 UI 분기 ---
                if (ownedCardInstance != null && !isInDeck)
                {
                    // 1. 보유 중 O, 덱 X (컬렉션에 표시, 클릭 가능)
                    cardImage.color = Color.white; // 밝게
                    cardButton.interactable = true;

                    // 리스너에는 '실제' 카드 인스턴스를 전달해야 함
                    CardData cardToMove = ownedCardInstance;
                    cardButton.onClick.AddListener(() => MoveCardToDeck(cardToMove));
                }
                else
                {
                    // 2. 보유 중 X (컬렉션에 어둡게, 클릭 불가)
                    // 3. 보유 중 O, 덱 O (컬렉션에 어둡게, 클릭 불가)
                    cardImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // 어둡게 (실루엣)
                    cardButton.interactable = false;
                }
            }
        }
        // ▲▲▲ [여기까지 수정] ▲▲▲


        // 2. 덱(활성 덱) 패널 새로 고침 (기존 로직 + 정렬 유지)
        foreach (Transform child in deckContent)
        {
            Destroy(child.gameObject);
        }

        // 숫자 우선, 문양 차선 정렬 (이전 요청)
        List<CardData> sortedDeck = player.activeDeck
                                        .OrderBy(card => card.number)
                                        .ThenBy(card => card.suit)
                                        .ToList();

        foreach (CardData cardInDeck in sortedDeck)
        {
            // CreateCardUI 함수는 덱 패널을 위해 계속 사용
            CreateCardUI(cardInDeck, deckContent, false); // false = "MoveToCollection" 리스너
        }
    }

    void CreateCardUI(CardData card, Transform parent, bool isMovingToDeck)
    {
        GameObject cardObj = Instantiate(cardPrefab, parent);

        Sprite cardSprite = GetCardSprite(card);

        Image cardImage = cardObj.GetComponent<Image>();

        if (cardImage != null && cardSprite != null)
        {
            cardImage.sprite = cardSprite;
        }

        Button cardButton = cardObj.GetComponent<Button>();
        if (cardButton)
        {
            cardButton.onClick.RemoveAllListeners();

            if (isMovingToDeck)
            {
                cardButton.onClick.AddListener(() => MoveCardToDeck(card));
            }
            else
            {
                cardButton.onClick.AddListener(() => MoveCardToCollection(card));
            }
        }
    }

    public void MoveCardToDeck(CardData card)
    {
        if (player.activeDeck.Count >= 5)
        {
            Debug.Log("덱이 꽉 찼습니다. (최대 5장)");
            return;
        }
        if (!player.activeDeck.Contains(card))
        {
            player.activeDeck.Add(card);
        }
        if (player.activeDeck.Count == 5)
        {
            CheckDeckCombination();
        }
        RefreshBothPanels();
    }
    public void MoveCardToCollection(CardData card)
    {
        if (player.activeDeck.Contains(card))
        {
            player.activeDeck.Remove(card);
        }
        RefreshBothPanels();
    }
    public void CheckDeckCombination()
    {
        if (player.activeDeck.Count != 5) return;

        List<CardData> deck = player.activeDeck;
        List<CardData> sortedDeck = deck.OrderBy(card => card.number).ToList();

        bool isStraight = true;
        for (int i = 0; i < sortedDeck.Count - 1; i++)
        {
            if (sortedDeck[i + 1].number != sortedDeck[i].number + 1)
            {
                isStraight = false;
                break;
            }
        }
        CardSuit firstSuit = deck[0].suit;
        bool isFlush = deck.All(card => card.suit == firstSuit);
        if (isStraight && isFlush)
        {
            Debug.Log("족보 달성: 스트레이트 플러시!");
        }
        else if (isFlush)
        {
            Debug.Log("족보 달성: 플러시!");
        }
        else if (isStraight)
        {
            Debug.Log("족보 달성: 스트레이트!");
        }
        else
        {
            var numberGroups = deck.GroupBy(card => card.number);

            bool hasFourOfAKind = numberGroups.Any(group => group.Count() == 4);
            bool hasThreeOfAKind = numberGroups.Any(group => group.Count() == 3);
            int pairCount = numberGroups.Count(group => group.Count() == 2);

            if (hasFourOfAKind)
            {
                Debug.Log("족보 달성: 포카드!"); // 
            }
            else if (hasThreeOfAKind && pairCount == 1)
            {
                Debug.Log("족보 달성: 풀하우스!"); // 
            }
            else if (hasThreeOfAKind)
            {
                Debug.Log("족보 달성: 트리플!");
            }
            else if (pairCount == 2)
            {
                Debug.Log("족보 달성: 투 페어!");
            }
            else if (pairCount == 1)
            {
                Debug.Log("족보 달성: 원 페어!");
            }
            else
            {
                Debug.Log("달성된 족보가 없습니다. (하이 카드)");
            }
        }
    }
    private Sprite GetCardSprite(CardData card)
    {
        // 1. 카드 번호(1~13)를 배열 인덱스(0~12)로 변환
        int numberIndex = card.number - 1;
        int finalIndex = 0; // allCardSprites 배열의 최종 인덱스

        // 2. 인덱스가 0~12 범위인지 확인
        if (numberIndex < 0 || numberIndex > 12)
        {
            Debug.LogError("잘못된 카드 번호입니다: " + card.number);
            return null;
        }

        // 3. 카드 문양(Suit)에 따라 인덱스 오프셋(Offset)을 계산
        // (사용자가 알려준 인덱스 규칙 적용)
        switch (card.suit)
        {
            // 하트: 0~12
            case CardSuit.Heart:
                finalIndex = 0 + numberIndex; // 0~12
                break;
            // 다이아몬드: 15~27
            case CardSuit.Diamond:
                finalIndex = 15 + numberIndex; // 15~27
                break;
            // 스페이드: 28~40
            case CardSuit.Spade:
                finalIndex = 28 + numberIndex; // 28~40
                break;
            // 클로버: 41~53
            case CardSuit.Clover:
                finalIndex = 41 + numberIndex; // 41~53
                break;
        }

        // 4. allCardSprites 배열에서 해당 스프라이트를 반환
        if (allCardSprites.Length > finalIndex)
        {
            return allCardSprites[finalIndex];
        }

        // 5. 예외 처리
        Debug.LogWarning(card.suit + " " + card.number + " (인덱스 " + finalIndex + ")에 해당하는 스프라이트를 찾지 못했습니다.");
        return null;
    }
    public void DisableWindow()
    {
        // 1. 창을 비활성화
        gameObject.SetActive(false);

        // 2. 게임 일시정지 상태 업데이트 (Time.timeScale = 1f로 복원)
        if (player != null)
        {
            player.UpdateGamePauseState();
        }
    }
}