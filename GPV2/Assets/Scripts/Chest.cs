using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Chest : MonoBehaviour
{
    private Animator animator;
    public GameObject choicePanel;
    public Player player;

    void Start()
    {
        player = FindObjectOfType<Player>();
        animator = this.GetComponent<Animator>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (player.Interaction)
            {
                animator.SetBool("Open", true);
            }
        }
    }

    public void ShowChoices()
    {
        choicePanel.SetActive(true); 
    }

    //래 함수들은 각 버튼의 OnClick() 이벤트에 연결

    public void OnWeaponSelected()
    {
        UnityEngine.Debug.Log("무기를 선택했습니다!");
        choicePanel.SetActive(false);
    }

    public void OnCardSelected()
    {
        UnityEngine.Debug.Log("카드를 선택했습니다!");

        choicePanel.SetActive(false);
    }

    public void OnPotionSelected()
    {
        UnityEngine.Debug.Log("물약을 선택했습니다!");
        choicePanel.SetActive(false);
    }
}
