using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject[] stages;
    public int stageIndex;
    public Player player;
    public Exit  exit;
    public Entrance entrance;
    public SceneFadeManager sfm;

    void Start()
    {
        player = FindObjectOfType<Player>();
    }

    public void NextStage()
    {
        sfm.NextLevelWithFade();

        if (stageIndex < stages.Length - 1)
        {
            stages[stageIndex].SetActive(false);
            stageIndex++;
            stages[stageIndex].SetActive(true);

            ExitReposition();
        }
        else
        {
            SceneManager.LoadScene("Stage2");
        }
    }

    public void PreviousStage()
    {
        if (stageIndex > 0)
        {
            stages[stageIndex].SetActive(false);
            stageIndex--;
            stages[stageIndex].SetActive(true);
            EntranceReposition();
        }
        else
        {
          return;
        }
    }

    void Update()
    {
        
    }

    void ExitReposition()
    {
        exit = FindObjectOfType<Exit>();
        player.VelocityZero();
        player.transform.position = exit.getCurrentPosition() + new Vector3 (0, -1.5f, 0);
    }

    void EntranceReposition()
    {
        entrance = FindObjectOfType<Entrance>();
        player.VelocityZero();
        player.transform.position = entrance.getCurrentPosition() + new Vector3(0, -1.5f, 0);
    }
}
