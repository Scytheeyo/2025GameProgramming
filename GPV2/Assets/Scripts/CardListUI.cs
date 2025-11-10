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

        foreach (Transform child in collectionContent)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in deckContent)
        {
            Destroy(child.gameObject);
        }

        foreach (CardData card in player.collectedCards)
        {
            if (player.activeDeck.Contains(card))
            {
                continue;
            }

            CreateCardUI(card, collectionContent, true);
        }

        foreach (CardData card in player.activeDeck)
        {
            CreateCardUI(card, deckContent, false);
        }
    }

    void CreateCardUI(CardData card, Transform parent, bool isMovingToDeck)
    {
        GameObject cardObj = Instantiate(cardPrefab, parent);

        TextMeshProUGUI suitText = cardObj.transform.Find("Text_Suit").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI numberText = cardObj.transform.Find("Text_Number").GetComponent<TextMeshProUGUI>();
        if (suitText) suitText.text = card.suit.ToString();
        if (numberText) numberText.text = card.number.ToString();

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
}