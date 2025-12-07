using UnityEngine;
using System.Collections;

public class CardTest : EnemyController_2D
{
    protected override void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform.Find("PlayerObject");
            }
            else
            {
                Debug.LogError("씬에 'Player' 태그를 가진 오브젝트가 없습니다!");
            }
        }

        float originalChance = cardDropChance;
        cardDropChance = 1.0f;

        Debug.Log("--- 카드 획득 테스트 시작 ---");

        System.Array suits = System.Enum.GetValues(typeof(CardSuit));

        for (int i = 0; i < 50; i++)
        {
            CardSuit randomSuit = (CardSuit)suits.GetValue(Random.Range(0, suits.Length));

            enemySuit = randomSuit;

            GiveCardToPlayer();
        }

        Debug.Log("--- 카드 획득 테스트 종료 ---");

        cardDropChance = originalChance;
    }

    protected override void Update(){ }

    public override void Die() { }
}