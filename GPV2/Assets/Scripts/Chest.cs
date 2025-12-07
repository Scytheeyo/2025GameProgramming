using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class Chest : MonoBehaviour
{
    private Animator animator;
    public GameObject choicePanel;
    public Player player;

    [Header("Reward Pools")]
    [Tooltip("획득 가능한 무기 프리팹들을 여기에 드래그해서 넣으세요")]
    public GameObject[] weaponPool; // 무기 풀 (인스펙터 할당용)
    public GameObject[] ItemPool; // 아이템 풀 (인스펙터 할당용)

    void Start()
    {
        player = FindObjectOfType<Player>();
        animator = this.GetComponent<Animator>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 상자가 열려있지 않을 때만 상호작용 (중복 실행 방지 로직 필요 시 추가)
            if (player.Interaction && !choicePanel.activeSelf)
            {
                animator.SetBool("Open", true);
                player.Interaction = false;
            }
        }
    }

    // 애니메이션 이벤트에서 호출한다고 가정 (상자가 다 열리면 선택지 표시)
    public void ShowChoices()
    {
        choicePanel.SetActive(true);
    }

    // --- 버튼 연결 함수들 ---

    public void OnWeaponSelected()
    {
        if (weaponPool.Length > 0)
        {
            int randomIndex = Random.Range(0, weaponPool.Length);
            GameObject selectedWeaponPrefab = weaponPool[randomIndex];

            GameObject weaponInstance = Instantiate(selectedWeaponPrefab);
            weaponInstance.name = selectedWeaponPrefab.name;
            string weaponName = weaponInstance.name;
            Weapon newWeapon = weaponInstance.GetComponent<Weapon>();

            if (newWeapon != null)
            {
                player.EquipWeapon(newWeapon);
            }

            Sprite weaponSprite = null;
            SpriteRenderer sr = selectedWeaponPrefab.GetComponent<SpriteRenderer>(); 

            if (sr != null)
            {
                weaponSprite = sr.sprite;
            }
            else
            {
                sr = selectedWeaponPrefab.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) weaponSprite = sr.sprite;
            }
            player.AddItemToInventory(weaponName, 1, weaponSprite);

            Debug.Log($"상자에서 획득 및 장착: {weaponName}");
        }

        CloseChestUI();
    }

    public void OnCardSelected()
    {
        // 1. 플레이어가 아직 가지고 있지 않은 카드 후보 리스트 생성
        List<CardData> missingCards = new List<CardData>();

        // 전체 카드 (4종류 문양 x 13개 숫자 = 52장)를 순회하면서 검사
        for (int s = 0; s < 4; s++)
        {
            for (int n = 1; n <= 13; n++)
            {
                CardSuit checkSuit = (CardSuit)s;
                int checkNum = n;

                // 플레이어의 수집 목록(collectedCards)에 이 카드가 있는지 확인
                // (클래스 비교이므로 속성값인 suit와 number로 비교해야 정확함)
                bool hasCard = player.collectedCards.Any(c => c.suit == checkSuit && c.number == checkNum);

                // 없다면 후보 리스트에 추가
                if (!hasCard)
                {
                    missingCards.Add(new CardData(checkSuit, checkNum));
                }
            }
        }

        // 2. 줄 수 있는 카드가 있는지 확인
        if (missingCards.Count > 0)
        {
            // 후보군 중에서 랜덤으로 인덱스 추첨
            int randomIndex = Random.Range(0, missingCards.Count);
            CardData selectedNewCard = missingCards[randomIndex];

            // 플레이어에게 지급
            player.AddCardToCollection(selectedNewCard);

            Debug.Log($"[상자 보상] 새로운 카드 획득! : {selectedNewCard.suit} - {selectedNewCard.number}");
        }
        else
        {
            // 3. (예외 처리) 플레이어가 이미 52장을 다 모은 경우
            Debug.LogWarning("모든 카드를 이미 수집했습니다! (대체 보상 지급 로직 필요)");

            // 예: 대신 물약을 주거나 골드를 주는 코드를 여기에 넣으세요.
            // player.AddItemToInventory("Gold", 100); 
        }

        CloseChestUI();
    }

    public void OnPotionSelected()
    {
        Debug.Log("물약을 선택했습니다!");
        if (ItemPool.Length > 0)
        {
            int randomIndex = Random.Range(0, ItemPool.Length);
            GameObject selectedPotionPrefab = ItemPool[randomIndex];
            string potionName = selectedPotionPrefab.name;
            Sprite potionSprite = null;
            SpriteRenderer sr = selectedPotionPrefab.GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                potionSprite = sr.sprite;
            }
            else
            {
                sr = selectedPotionPrefab.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) potionSprite = sr.sprite;
            }

            player.AddItemToInventory(potionName, 1, potionSprite);

            Debug.Log($"상자에서 획득: {potionName}");
        }
        CloseChestUI();
    }
    private void CloseChestUI()
    {
        choicePanel.SetActive(false);
        gameObject.SetActive(false); 
    }
}