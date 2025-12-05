using UnityEngine;
using Cinemachine;
using System.Collections;

public class AutoCamBinder : MonoBehaviour
{
    // 반복문 탈출을 위한 검사 변수
    Player playerScript = null;

    IEnumerator Start()
    {
        var vcam = GetComponent<CinemachineVirtualCamera>();

        // 진짜 Player 스크립트를 찾을 때까지 무한 반복
        while (playerScript == null)
        {
            // 1. 'Player' 태그가 달린 껍데기(Root)를 먼저 찾습니다.
            GameObject rootObj = GameObject.FindGameObjectWithTag("Player");

            if (rootObj != null)
            {
                // 2. 껍데기 안에서 진짜 알맹이인 "PlayerObject" 자식을 찾습니다.
                Transform realPlayerTransform = rootObj.transform.Find("PlayerObject");

                if (realPlayerTransform != null)
                {
                    // 3. ★ 중요: Player 스크립트는 자식에게 있으므로 자식에서 가져옵니다.
                    playerScript = realPlayerTransform.GetComponent<Player>();

                    // 4. 카메라도 자식을 따라가게 설정합니다.
                    vcam.Follow = realPlayerTransform;

                    Debug.Log("연결 성공: PlayerObject를 찾았습니다!");
                }
                else
                {
                    Debug.LogWarning($"'Player' 태그 객체는 찾았는데, 그 아래 'PlayerObject'라는 이름의 자식이 없습니다!");
                }
            }

            // 못 찾았으면 0.1초 대기 후 재시도
            yield return new WaitForSeconds(0.1f);
        }
    }
}