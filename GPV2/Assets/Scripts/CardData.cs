using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum CardSuit
{
    Spade,   // 공격력
    Heart,   // 체력
    Diamond, // 방어력
    Clover   // 마나
}

public class CardData
{
    public CardSuit suit;
    public int number;

    public CardData(CardSuit s, int num)
    {
        suit = s;
        number = num;
    }
}