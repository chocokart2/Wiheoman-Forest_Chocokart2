using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
///     1스테이지 보스 클래스입니다.
/// </summary>
/// <remarks>
///     코딩 설계 스타일은 반드시 융통성 있게, 다시말해서 요구사항이 바뀌어도 금방 적용이 가능한 "일반적인" 함수들을 기반으로 로직을 작성하셔야 합니다.
/// </remarks>
public class BossFlorainaController : EnemyBase
{
    [SerializeField] private float maxHealth;
    [SerializeField] private float subHealth;
    [SerializeField] private float thresholdHealthRateSummon = 0.5f;
    [SerializeField] private float thresholdHealthRateHealing = 0.0f;
    [SerializeField] private float healthRateHealed = 0.3f;
    [SerializeField] private float attackBeginTime = 1.0f;
    [SerializeField] private float attackPeriod = 3.0f;
    [SerializeField] private float timeForFlowerSpawning = 3.0f;
    [SerializeField] private float timeForHealing = 10.0f;
    [SerializeField] private float timeForBerryMove = 2.0f;
    [SerializeField] private float chanceOfRootAttack = 0.7f;
    [SerializeField] private float chanceOfBerryBomb = 0.3f;
    private Coroutine attackCoroutine; // 일반적으로는 켜져 있음. 꽃몬 소환할떄 잠시 꺼두고, 소환 다 되었으면 다시 실행.
    private Coroutine flownerMonSpawnCoroutine;
    private Coroutine healingCoroutine;
    [SerializeField] private GameObject prefabRoot;
    [SerializeField] private GameObject prefabBerryBomb;
    [SerializeField] private GameObject prefabFlowerMon;
    [SerializeField] private float flowerMonSpawnPositionMaxX;
    [SerializeField] private float flowerMonSpawnPositionMinX;
    private bool isHealing = false;
    [SerializeField] private float positionTopYofBerryCenter;
    [SerializeField] private Vector3 positionMinOfBerrySpawn;
    [SerializeField] private Vector3 positionMaxOfBerrySpawn;
    [SerializeField] private Vector3 positionMinOfBerryDestination;
    [SerializeField] private Vector3 positionMaxOfBerryDestination;
    // 이 세가지 변수는 보스의 상태를 보여주기 위해 임시로 보여주는 머티리얼입니다.
    private MeshRenderer myMeshRenderer;
    [SerializeField] private Color TEMP_colorDefault; // 기본 상태
    [SerializeField] private Color TEMP_colorReadyRootAttack; // 뿌리 공격을 하는 애니메이션 + 뿌리 공격 전조로 대체될 예정
    [SerializeField] private Color TEMP_colorSelfHealing; // 하얀색 스스로 힐하는 이펙트
    [SerializeField] private Color TEMP_colorDead; // 사망 이펙트

    // Start is called before the first frame update
    void Start()
    {
        stat.rank = ERank.boss;
        stat.health = maxHealth;

        attackCoroutine = StartCoroutine(AttackPattern());
        myMeshRenderer = GetComponent<MeshRenderer>();
        myMeshRenderer.material.color = TEMP_colorDefault;
    }

    protected override void DoDeathHandle()
    {
        StopAllCoroutines();
        myMeshRenderer.material.color = TEMP_colorDead;
        Debug.Log("플로라이나 사망");
        // 아이템 드랍
    }

    protected override void DoInjuryHandle(PlayerAttackParameters parameter)
    {
        if (isHealing)
        {
            if (parameter.attackType != EPlayerAttackType.heavyAttack)
            {
                return;
            }
            subHealth -= parameter.damage;
        }

        // 체력이 임계값보다 낮아지면, 코루틴 제거 후 꽃몬소환 후 공격재개
        if ((stat.health + parameter.damage > thresholdHealthRateSummon * maxHealth) && (stat.health <= thresholdHealthRateSummon * maxHealth))
        {
            Debug.Log("소환 루틴");
            StopCoroutine(attackCoroutine);
            // 꽃몬 소환 코루틴
            flownerMonSpawnCoroutine = StartCoroutine(SpawnEnemy());

                
        }
        else if ((stat.health + parameter.damage > thresholdHealthRateHealing * maxHealth) && (stat.health <= thresholdHealthRateHealing * maxHealth))
        {
            Debug.Log("회복 루틴");
            stat.health = 0.0f;
            StopCoroutine(attackCoroutine);
            StopCoroutine(flownerMonSpawnCoroutine); // 안전하게 하기 위해 만에 하나 시작하면 종료
            // 회복 루틴
            healingCoroutine = StartCoroutine(HealSelf());
        }
    }

    protected override bool IsDeathAllowed()
    {
        return isHealing && (subHealth < -150);
    }

    private IEnumerator AttackPattern()
    {
        yield return new WaitForSeconds(attackBeginTime);
        while (true)
        {
            // 공격 뭐 할지 선택
            // 그 다음 공격 함수 호출
            
            float randomNumber = UnityEngine.Random.Range(0.0f, chanceOfRootAttack + chanceOfBerryBomb);

            if (randomNumber < chanceOfRootAttack)
            {
                StartCoroutine(AttackWithRoot());
            }
            else
            {
                AttackWithBerryBomb();
            }

            yield return new WaitForSeconds(attackPeriod);
        }
    }

    private IEnumerator AttackWithRoot()
    {
        myMeshRenderer.material.color = TEMP_colorReadyRootAttack;

        Instantiate(
            prefabRoot, 
            new Vector3(playerGameObject.transform.position.x, 1.5f, 0),
            prefabRoot.transform.rotation);
        yield return new WaitForSeconds(0.5f);

        myMeshRenderer.material.color = TEMP_colorDefault;
    }

    private void AttackWithBerryBomb()
    {
        // 민혁씨 요구사항이 완전히 결정되지 않음. 로직 바뀔 예정
        //
        for (int berryNumner = 0; berryNumner < 3; ++berryNumner)
        {
            GameObject berryBomb = 
                Instantiate(
                    prefabBerryBomb, 
                    playerGameObject.transform.position, 
                    prefabBerryBomb.transform.rotation);
            StartCoroutine(BerryMove(berryBomb));
            Vector3 berryStart = new Vector3(
                Random.Range(positionMinOfBerrySpawn.x, positionMaxOfBerrySpawn.x),
                Random.Range(positionMinOfBerrySpawn.y, positionMaxOfBerrySpawn.y),
                0);
            Vector3 berryEnd = new Vector3(
                Random.Range(positionMinOfBerryDestination.x, positionMaxOfBerryDestination.x),
                Random.Range(positionMinOfBerryDestination.y, positionMaxOfBerryDestination.y),
                0);
            Vector3 berryCenter = new Vector3(
                (berryStart.x + berryEnd.x) / 2.0f,
                positionTopYofBerryCenter,
                0);
            StartCoroutine(GeometryUtility.MoveOnBezierCurve(berryStart, berryEnd, berryCenter, berryBomb, timeForBerryMove));
        }
    }

    /// <summary>
    ///     베지어 곡선을 따라 베리가 움직이는 함수
    /// </summary>
    /// <param name="moveObject">움직일 대상입니다.</param>
    /// <returns></returns>
    /// <remarks>참조 링크 : https://leekangw.github.io/posts/49/</remarks>
    private IEnumerator BerryMove(GameObject moveObject)
    {
        // 출발지점 S와 도착지점 D를 지정합니다. 그리고 출빌지점과 도착지점을 잇는 포물선 함수를 생성합니다.
        Vector3 berryStart = new Vector3(
            Random.Range(positionMinOfBerrySpawn.x, positionMaxOfBerrySpawn.x),
            Random.Range(positionMinOfBerrySpawn.y, positionMaxOfBerrySpawn.y),
            0);
        Vector3 berryEnd = new Vector3(
            Random.Range(positionMinOfBerryDestination.x, positionMaxOfBerryDestination.x),
            Random.Range(positionMinOfBerryDestination.y, positionMaxOfBerryDestination.y),
            0);
        Vector3 berryCenter = new Vector3(
            (berryStart.x + berryEnd.x) / 2.0f,
            positionTopYofBerryCenter,
            0);
        float time = 0;

        while (time < timeForBerryMove)
        {
            // 부동소수점 오차도 문제 때문에 time += Time.deltaTime / timeForBerryMove 로직을 쓰지 않음
            float lerpValue = time / timeForBerryMove;
            Vector2 pointStartToCenter = Vector3.Lerp(berryStart, berryCenter, lerpValue);
            Vector2 pointCenterToEnd = Vector3.Lerp(berryCenter, berryEnd, lerpValue);
            moveObject.transform.position = Vector3.Lerp(pointStartToCenter, pointCenterToEnd, lerpValue);

            time += Time.deltaTime;
            yield return null;
        }

        moveObject.transform.position = berryEnd; // 베리가 도착 지점에 정확히 위치함을 보장한다.
    }
    
    /// <summary>
    ///     적 유닛을 소환해야 하는 경우 호출합니다.
    /// </summary>
    private IEnumerator SpawnEnemy()
    {
        // 꽃이 소환됨
        // 꽃이 떨어지는 중
        yield return new WaitForSeconds(timeForFlowerSpawning);
        Vector3 position = new Vector3(
            UnityEngine.Random.Range(flowerMonSpawnPositionMinX, flowerMonSpawnPositionMaxX), 1, 0);

        Instantiate(prefabFlowerMon, position, prefabFlowerMon.transform.rotation);
        // 코루틴 복귀
        attackCoroutine = StartCoroutine(AttackPattern());
    }

    private IEnumerator HealSelf()
    {
        Debug.Log("회복 시작");
        isHealing = true;
        myMeshRenderer.material.color = TEMP_colorSelfHealing;

        yield return new WaitForSeconds(timeForHealing);
        
        Debug.Log("회복 종료");
        isHealing = false;
        stat.health = maxHealth * healthRateHealed;
        subHealth = 0;
        attackCoroutine = StartCoroutine(AttackPattern());
        myMeshRenderer.material.color = TEMP_colorDefault;
    }
}
