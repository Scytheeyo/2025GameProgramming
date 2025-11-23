using UnityEngine;

public class Door : MonoBehaviour
{
    public GameObject nextStage;
    public Transform targetEntrance;
    public Cinemachine.CinemachineVirtualCamera nextRoomVcam;

    public void InitiateTransition()
    {
        
        //GetComponent<AudioSource>().Play(); 

        if (GameManager.instance != null)
        {
            GameManager.instance.MoveToNextStage(this);
        }
        else
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다.");
        }
    }
}