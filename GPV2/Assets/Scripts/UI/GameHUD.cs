using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class GameHUD : MonoBehaviour
{
    [Header("Player Reference")]
    public Player player;

    [Header("Card Count Texts (TMP)")]
    public TextMeshProUGUI spadeCountText;
    public TextMeshProUGUI heartCountText;
    public TextMeshProUGUI diamondCountText;
    public TextMeshProUGUI cloverCountText;

    [Header("Status Bars")]
    public Slider hpSlider;
    public Slider mpSlider;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mpText;

    // 최적화 변수
    private int lastSpade = -1;
    private int lastHeart = -1;
    private int lastDiamond = -1;
    private int lastClover = -1;

    private int lastHP = -1;
    private int lastMP = -1;
    // [추가됨] 최대 체력/마나가 변할 수 있으므로 변경 감지를 위해 변수 추가
    private int lastMaxHP = -1;
    private int lastMaxMP = -1;

    void Start()
    {
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
    }

    void Update()
    {
        if (player == null) return;

        UpdateCardCounts();
        UpdateStatusBars();
    }

    void UpdateCardCounts()
    {
        int currentSpade = player.collectedCards.Count(c => c.suit == CardSuit.Spade);
        int currentHeart = player.collectedCards.Count(c => c.suit == CardSuit.Heart);
        int currentDiamond = player.collectedCards.Count(c => c.suit == CardSuit.Diamond);
        int currentClover = player.collectedCards.Count(c => c.suit == CardSuit.Clover);

        if (currentSpade != lastSpade)
        {
            if (spadeCountText != null) spadeCountText.text = currentSpade.ToString();
            lastSpade = currentSpade;
        }

        if (currentHeart != lastHeart)
        {
            if (heartCountText != null) heartCountText.text = currentHeart.ToString();
            lastHeart = currentHeart;
        }

        if (currentDiamond != lastDiamond)
        {
            if (diamondCountText != null) diamondCountText.text = currentDiamond.ToString();
            lastDiamond = currentDiamond;
        }

        if (currentClover != lastClover)
        {
            if (cloverCountText != null) cloverCountText.text = currentClover.ToString();
            lastClover = currentClover;
        }
    }

    // [수정됨] Player 클래스 이름 대신 player 인스턴스 변수 사용
    void UpdateStatusBars()
    {
        // 1. HP 갱신 (현재 체력이나 최대 체력이 바뀌었을 때)
        // Player.Max_Health -> player.maxHealth 로 변경됨
        if (player.health != lastHP || player.maxHealth != lastMaxHP)
        {
            if (hpSlider != null)
            {
                // player.maxHealth를 사용하여 비율 계산
                hpSlider.value = (float)player.health / player.maxHealth;
            }

            if (hpText != null)
            {
                // 텍스트에도 player.maxHealth 사용
                hpText.text = $"{player.health} / {player.maxHealth}";
            }

            lastHP = player.health;
            lastMaxHP = player.maxHealth;
        }

        // 2. MP 갱신 (현재 마나나 최대 마나가 바뀌었을 때)
        // Player.Max_Mana -> player.maxMana 로 변경됨
        if (player.mana != lastMP || player.maxMana != lastMaxMP)
        {
            if (mpSlider != null)
            {
                // player.maxMana를 사용하여 비율 계산
                mpSlider.value = (float)player.mana / player.maxMana;
            }

            if (mpText != null)
            {
                // 텍스트에도 player.maxMana 사용
                mpText.text = $"{player.mana} / {player.maxMana}";
            }

            lastMP = (int) player.mana;
            lastMaxMP = player.maxMana;
        }
    }
}