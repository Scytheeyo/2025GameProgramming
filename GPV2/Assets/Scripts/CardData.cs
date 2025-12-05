using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum CardSuit
{
    Spade,   // ���ݷ�
    Heart,   // ü��
    Diamond, // ����
    Clover   // ����
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