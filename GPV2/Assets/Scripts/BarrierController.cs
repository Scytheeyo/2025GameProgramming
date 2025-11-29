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

        // ==========================================
        // 1. 장벽 생성 (Start) & 몬스터 소환
        // ==========================================
        GameObject startInstance = Instantiate(barrierStartPrefab, barrierPosition.position, barrierPosition.rotation);

        // *몬스터 소환 실행*
        SpawnMonsters();

        // Start 애니메이션 길이만큼 대기
        float startDuration = GetEffectDuration(startInstance);
        yield return new WaitForSeconds(startDuration);

        Destroy(startInstance);

        // ==========================================
        // 2. 장벽 유지 (Loop) & 전투 단계
        // ==========================================
        GameObject loopInstance = Instantiate(barrierLoopPrefab, barrierPosition.position, barrierPosition.rotation);

        // 소환된 몬스터들이 다 죽을 때까지 대기
        // (Enemy 태그 전체를 찾는 게 아니라, *이 기믹에서 소환된 애들*만 감시합니다)
        while (AreMonstersAlive())
        {
            yield return new WaitForSeconds(0.5f);
        }

        Destroy(loopInstance);

        // ==========================================
        // 3. 장벽 해제 (End)
        // ==========================================
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
        // 설정된 스폰 포인트 개수만큼 돌거나, 몬스터 프리팹 개수만큼 돕니다.
        // 여기서는 '스폰 포인트' 개수를 기준으로 합니다.
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            // 소환할 몬스터 결정 (프리팹 배열에서 순서대로 혹은 랜덤으로)
            // 여기서는 순서대로 하되, 프리팹이 부족하면 첫 번째 몬스터를 계속 씁니다.
            GameObject prefabToSpawn = (i < monsterPrefabs.Length) ? monsterPrefabs[i] : monsterPrefabs[0];

            if (prefabToSpawn != null && spawnPoints[i] != null)
            {
                GameObject newMonster = Instantiate(prefabToSpawn, spawnPoints[i].position, spawnPoints[i].rotation);

                // 추적 리스트에 추가
                spawnedMonsters.Add(newMonster);
            }
        }
    }

    // 소환된 몬스터가 살아있는지 체크하는 함수
    private bool AreMonstersAlive()
    {
        // 리스트를 역순으로 돌면서 죽은(null이 된) 몬스터는 리스트에서 뺍니다.
        for (int i = spawnedMonsters.Count - 1; i >= 0; i--)
        {
            if (spawnedMonsters[i] == null)
            {
                spawnedMonsters.RemoveAt(i);
            }
        }

        // 리스트에 남은 몬스터가 0마리보다 많으면 아직 살아있는 것
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