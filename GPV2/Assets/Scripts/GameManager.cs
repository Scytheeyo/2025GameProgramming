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

    public Player player;
    public GameObject currentStage; 
    public static GameManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (currentStage == null)
        {
                return;
        }
    }
    public void MoveToNextStage(Door transitionDoor)
    {
        // 연결된 방이 없는 경우 방어 코드
        if (transitionDoor.nextStage == null)
        {
            return;
        }

        if (currentStage != null)
        {
            currentStage.SetActive(false);
        }

        GameObject newStage = transitionDoor.nextStage;
        newStage.SetActive(true); 
        currentStage = newStage;

        RepositionPlayer(transitionDoor.targetEntrance.position);
    }

    void RepositionPlayer(Vector3 targetPosition)
    {
        player.VelocityZero();
        player.transform.position = targetPosition;
    }
}