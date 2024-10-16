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
    [SerializeField] private GameObject prefabFallingFlower;
    [SerializeField] private GameObject prefabFlowerMonMelee;
    [SerializeField] private GameObject prefabFlowerMonRanged;
    [SerializeField] private float flowerMonSpawnPositionMaxX;
    [SerializeField] private float flowerMonSpawnPositionMinX;
    private bool isHealing = false;
    [SerializeField] private float positionTopYofFlowerCenter;
    [SerializeField] private float positionTopYofBerryCenter;
    [SerializeField] private float positionYBeyondCamera;
    [SerializeField] private Vector3 positionMinOfFlowerSpawn;
    [SerializeField] private Vector3 positionMaxOfFlowerSpawn;
    [SerializeField] private Vector3 positionMinOfBerrySpawn;
    [SerializeField] private Vector3 positionMaxOfBerrySpawn;
    [SerializeField] private Vector3 positionMinOfBerryDestination;
    [SerializeField] private Vector3 positionMaxOfBerryDestination;
    [SerializeField] private int countOfSpawningFlowerMonMelee;
    [SerializeField] private int countOfSpawningFlowerMonRanged;

    // Start is called before the first frame update
    void Start()
    {
        stat.rank = ERank.boss;
        stat.health = maxHealth;

        attackCoroutine = StartCoroutine(AttackPattern());
    }

    protected override void DoDeathHandle()
    {
        StopAllCoroutines();

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
                AttackWithRoot();
            }
            else
            {
                AttackWithBerryBomb();
            }

            yield return new WaitForSeconds(attackPeriod);
        }
    }

    private void AttackWithRoot()
    {
        Instantiate(
            prefabRoot,
            new Vector3(playerGameObject.transform.position.x, 1.5f, 0),
            prefabRoot.transform.rotation);
    }

    private void AttackWithBerryBomb()
    {
        // 민혁씨 요구사항이 완전히 결정되지 않음. 로직 바뀔 예정
        for (int berryNumner = 0; berryNumner < 3; ++berryNumner)
        {
            GameObject berryBomb =
                Instantiate(
                    prefabBerryBomb,
                    playerGameObject.transform.position,
                    prefabBerryBomb.transform.rotation);
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
            StartCoroutine(UtilityFunctions.MoveOnBezierCurve(berryStart, berryEnd, berryCenter, berryBomb, timeForBerryMove));
            StartCoroutine(UtilityFunctions.RunAfterDelay(
                timeForBerryMove,
                () =>
                {
                    berryBomb.GetComponent<FlorainaBerryBombController>().Land();
                }));
        }
    }

    /// <summary>
    ///     적 유닛을 소환해야 하는 경우 호출합니다.
    /// </summary>
    private IEnumerator SpawnEnemy()
    {
        // 꽃이 소환됨
        GameObject[] flowers = new GameObject[countOfSpawningFlowerMonMelee + countOfSpawningFlowerMonRanged];

        // 무작위 위치에 적 생성

        // 1번째 코드
        //for (int index = 0; index < flowers.Length; ++index)
        //{
        //    Vector3 oneStartPosition = new Vector3(
        //        UnityEngine.Random.Range(positionMinOfFlowerSpawn.x, positionMaxOfFlowerSpawn.x),
        //        UnityEngine.Random.Range(positionMinOfFlowerSpawn.y, positionMaxOfFlowerSpawn.y),
        //        0);
        //    Vector3 oneEndPosition = new Vector3(
        //        UnityEngine.Random.Range(flowerMonSpawnPositionMinX, flowerMonSpawnPositionMaxX), 1, 0);
        //    Vector3 oneCenterPosition = new Vector3(
        //        (oneStartPosition.x + oneEndPosition.x) / 2.0f,
        //        positionTopYofFlowerCenter,
        //        0);
        //    flowers[index] = Instantiate(prefabFallingFlower, oneStartPosition, prefabFallingFlower.transform.rotation);
        //    StartCoroutine(UtilityFunctions.MoveOnBezierCurve(oneStartPosition, oneEndPosition, oneCenterPosition, flowers[index], timeForFlowerSpawning));
        //}
        //// 꽃이 떨어지는 중
        //yield return new WaitForSeconds(timeForFlowerSpawning);

        // 2번째 코드
        
        for (int index = 0; index < flowers.Length; ++index)
        {
            Vector3 oneStartPosition = new Vector3(
                UnityEngine.Random.Range(positionMinOfFlowerSpawn.x, positionMaxOfFlowerSpawn.x),
                UnityEngine.Random.Range(positionMinOfFlowerSpawn.y, positionMaxOfFlowerSpawn.y),
                0);
            Vector3 oneEndPosition = new Vector3(
                UnityEngine.Random.Range(flowerMonSpawnPositionMinX, flowerMonSpawnPositionMaxX), positionYBeyondCamera, 0);
            Vector3 oneCenterPosition = new Vector3(
                oneStartPosition.x, positionYBeyondCamera, 0);
            flowers[index] = Instantiate(prefabFallingFlower, oneStartPosition, prefabFallingFlower.transform.rotation);
            StartCoroutine(UtilityFunctions.MoveOnBezierCurve(oneStartPosition, oneEndPosition, oneCenterPosition, flowers[index], timeForFlowerSpawning));
        }
        yield return new WaitForSeconds(timeForFlowerSpawning);

        for (int index = 0; index < countOfSpawningFlowerMonMelee; ++index)
        {
            Instantiate(prefabFlowerMonMelee, flowers[index].transform.position, prefabFlowerMonMelee.transform.rotation);
            Destroy(flowers[index]);
        }
        for (int index = countOfSpawningFlowerMonMelee; index < flowers.Length; ++index)
        {
            Instantiate(prefabFlowerMonRanged, flowers[index].transform.position, prefabFlowerMonRanged.transform.rotation);
            Destroy(flowers[index]);
        }
        // 코루틴 복귀
        attackCoroutine = StartCoroutine(AttackPattern());
    }

    private IEnumerator HealSelf()
    {
        Debug.Log("회복 시작");
        isHealing = true;
        yield return new WaitForSeconds(timeForHealing);
        Debug.Log("회복 종료");
        isHealing = false;
        stat.health = maxHealth * healthRateHealed;
        subHealth = 0;
        attackCoroutine = StartCoroutine(AttackPattern());
    }
}
