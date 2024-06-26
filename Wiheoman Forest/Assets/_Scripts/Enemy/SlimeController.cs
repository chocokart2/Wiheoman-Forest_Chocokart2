using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SlimeController : EnemyBase
{
    [SerializeField]
    private float jumpInterval;
    private Coroutine jumpCoroutine;
    [SerializeField]
    private float jumpForce;
    [SerializeField]
    private float moveForce;

    // Start is called before the first frame update
    void Start()
    {
        enemyRigidbody = GetComponent<Rigidbody>();
        jumpCoroutine = StartCoroutine(Jump());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    protected override void DoDeathHandle()
    {
        // ===============================
        // 슬라임이 사망했으므로 더이상 점프를 하지 않습니다.
        // ===============================
        StopCoroutine(jumpCoroutine);
        Debug.Log("슬라임이 사망했습니다.");
    }
    // ===============================
    // TODO : 해당 슬라임이 점프를 해서 튀어오르는 동안엔
    // 슬라임이 플레이어에게 닿게 된 경우 데미지가 들어가도록 해야 합니다.
    // 하지만 플레이어에게 데미지를 주는 함수가 없고 플레이어 체력에 관여하는 어떤 통일된 표준이 없어요.
    // =============================== 
    /// <summary>
    ///     슬라임이 jumpInterval 만큼의 주기로 통통 튀어오르는 함수입니다.
    /// </summary>
    /// <returns></returns>
    private IEnumerator Jump()
    {
        while (true)
        {
            yield return new WaitForSeconds(jumpInterval);
            if (enemyRigidbody == null)
            {
                Debug.LogError("SlimeController.AttackPlayer()에서 리지드바디를 찾을 수 없습니다.");
            }
            Debug.Log($"found player : {IsFoundPlayer()}");
            // 점프
            enemyRigidbody.AddForce(
                new Vector3(0, jumpForce, 0) + GetPseudoDirection() * moveForce, 
                ForceMode.VelocityChange);
        }
    }
}
