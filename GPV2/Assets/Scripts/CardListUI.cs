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
                CardData slotCardData = new CardData(suit, number);

                // [수정 1] 해당 문양과 숫자를 가진 카드가 총 몇 장인지 계산
                int cardCount = player.collectedCards.Count(c => c.suit == suit && c.number == number);

                // [수정 2] 버튼 기능 연결을 위해 '첫 번째' 카드 인스턴스를 가져옴 (없으면 null)
                CardData firstInstance = player.collectedCards.FirstOrDefault(c => c.suit == suit && c.number == number);

                // 덱에 포함되어 있는지 확인 (한 장이라도 덱에 있으면 표시)
                bool isInDeck = (firstInstance != null) && player.activeDeck.Contains(firstInstance);

                // --- UI 오브젝트 생성 ---
                GameObject cardObj = Instantiate(cardPrefab, collectionContent);
                Image cardImage = cardObj.GetComponent<Image>();
                Button cardButton = cardObj.GetComponent<Button>();

                // [수정 3] 수량 텍스트 찾기 및 설정
                // 프리팹에 만들어둔 "CountText"라는 이름의 자식 오브젝트를 찾습니다.
                Transform countTextTrans = cardObj.transform.Find("CountText");
                if (countTextTrans != null)
                {
                    TextMeshProUGUI countText = countTextTrans.GetComponent<TextMeshProUGUI>();
                    if (countText != null)
                    {
                        // 2장 이상일 때만 "2", "3" 표시 (1장이나 0장은 숨김 처리)
                        if (cardCount >= 1)
                        {
                            countText.text = "" + cardCount;
                            countText.gameObject.SetActive(true);
                        }
                        else
                        {
                            countText.gameObject.SetActive(false);
                        }
                    }
                }

                Sprite cardSprite = GetCardSprite(slotCardData);
                if (cardImage != null && cardSprite != null)
                {
                    cardImage.sprite = cardSprite;
                }

                cardButton.onClick.RemoveAllListeners();

                // --- 상태 분기 ---
                // 카드를 1장 이상 가지고 있고, 덱에 없다면 덱으로 보낼 수 있음
                if (cardCount > 0 && !isInDeck)
                {
                    cardImage.color = Color.white;
                    cardButton.interactable = true;

                    // 첫 번째 인스턴스를 덱으로 이동시킴
                    cardButton.onClick.AddListener(() => MoveCardToDeck(firstInstance));
                }
                else
                {
                    // 카드가 없거나(0장), 이미 덱에 있는 경우
                    cardImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // 어둡게
                    cardButton.interactable = false;
                }
            }
        }

        // 2. 덱(활성 덱) 패널 새로 고침 (기존 로직 유지)
        foreach (Transform child in deckContent)
        {
            Destroy(child.gameObject);
        }

        List<CardData> sortedDeck = player.activeDeck
                                        .OrderBy(card => card.number)
                                        .ThenBy(card => card.suit)
                                        .ToList();

        foreach (CardData cardInDeck in sortedDeck)
        {
            CreateCardUI(cardInDeck, deckContent, false);
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