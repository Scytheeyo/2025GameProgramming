using System.Collections;
using System.Collections.Generic; // 리스트 사용을 위해 추가
using UnityEngine;

public class BarrierController : MonoBehaviour
{
    [Header("Barrier Animations")]
    public GameObject barrierStartPrefab; // 1단계: 준비
    public GameObject barrierLoopPrefab;  // 2단계: 유지
    public GameObject barrierEndPrefab;   // 3단계: 해제

    [Header("Monster Settings")]
    [Tooltip("소환할 몬스터 프리팹들 (여러 종류 가능)")]
    public GameObject[] monsterPrefabs;

    [Tooltip("몬스터가 소환될 위치들 (빈 오브젝트로 위치 지정)")]
    public Transform[] spawnPoints;

    [Header("General Settings")]
    public Transform barrierPosition; // 장벽 위치
    private bool isActivated = false;

    // 소환된 몬스터들을 추적하기 위한 리스트
    private List<GameObject> spawnedMonsters = new List<GameObject>();

    private void Start()
    {
        if (barrierPosition == null) barrierPosition = this.transform;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어인지, 그리고 이미 발동된 적이 없는지 확인
        if (other.CompareTag("Player") && !isActivated)
        {
            // 리지드바디 체크 등 물리 오류 방지용 로그
            Debug.Log("이벤트 발동! 마법 장벽과 몬스터를 소환합니다.");
            StartCoroutine(SequenceRoutine());
        }
    }

    private IEnumerator SequenceRoutine()
    {
        isActivated = true; // 중복 실행 방지
        GameObject startInstance = Instantiate(barrierStartPrefab, barrierPosition.position, barrierPosition.rotation);

        // *몬스터 소환 실행*
        SpawnMonsters();

        float startDuration = GetEffectDuration(startInstance);
        yield return new WaitForSeconds(startDuration);

        Destroy(startInstance);

        GameObject loopInstance = Instantiate(barrierLoopPrefab, barrierPosition.position, barrierPosition.rotation);

        while (AreMonstersAlive())
        {
            yield return new WaitForSeconds(0.5f);
        }

        Destroy(loopInstance);

        if (barrierEndPrefab != null)
        {
            GameObject endInstance = Instantiate(barrierEndPrefab, barrierPosition.position, barrierPosition.rotation);
            yield return new WaitForSeconds(GetEffectDuration(endInstance));
            Destroy(endInstance);
        }

        // 모든 기믹 종료 -> 트리거 삭제
        Destroy(gameObject);
    }

    // 몬스터 소환 함수
    private void SpawnMonsters()
    {

        for (int i = 0; i < spawnPoints.Length; i++)
        {

            GameObject prefabToSpawn = (i < monsterPrefabs.Length) ? monsterPrefabs[i] : monsterPrefabs[0];

            if (prefabToSpawn != null && spawnPoints[i] != null)
            {
                GameObject newMonster = Instantiate(prefabToSpawn, spawnPoints[i].position, spawnPoints[i].rotation);

                spawnedMonsters.Add(newMonster);
            }
        }
    }

    private bool AreMonstersAlive()
    {
        for (int i = spawnedMonsters.Count - 1; i >= 0; i--)
        {
            if (spawnedMonsters[i] == null)
            {
                spawnedMonsters.RemoveAt(i);
            }
        }
        return spawnedMonsters.Count > 0;
    }

    private float GetEffectDuration(GameObject obj)
    {
        Animator anim = obj.GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null && anim.runtimeAnimatorController.animationClips.Length > 0)
            return anim.runtimeAnimatorController.animationClips[0].length;

        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        if (ps != null) return ps.main.duration;

        return 2.0f;
    }
}