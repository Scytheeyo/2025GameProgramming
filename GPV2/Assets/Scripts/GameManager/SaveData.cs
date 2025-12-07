using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // 1. 기본 정보
    public int mapSeed; // ★ 핵심: 이 번호만 있으면 맵이 똑같이 만들어짐
    public int roomIndex; // 몇 번째 방인지 (순서)

    public Vector3 playerPosition;
    public int currentHealth;
    public float currentMana;

    // 2. 카드 목록 (CardData는 직렬화가 안 되어 있을 수 있으므로 전용 구조체 사용)
    public List<CardSaveData> collectedCards = new List<CardSaveData>();

    // 3. 인벤토리 (Dictionary는 저장이 안 되므로 List로 변환하여 저장)
    public List<InventorySaveData> inventoryItems = new List<InventorySaveData>();
}

// 카드 저장을 위한 간단한 구조체
[System.Serializable]
public struct CardSaveData
{
    public CardSuit suit;
    public int number;
}

// 인벤토리 아이템 저장을 위한 구조체 (Key, Value 쌍)
[System.Serializable]
public struct InventorySaveData
{
    public string itemName; // 아이템 태그 (Key)
    public int amount;      // 수량 (Value)
}