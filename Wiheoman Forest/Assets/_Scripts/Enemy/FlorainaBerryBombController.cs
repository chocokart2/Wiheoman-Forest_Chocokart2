using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlorainaBerryBombController : MonoBehaviour
{
    [SerializeField] float beginWaitTime = 5.0f;
    [SerializeField] float remainTime = 0.3f;
    [SerializeField] float playerPushBerryDistance;
    [SerializeField] float timeForPlayerPushBerry;
    bool isPushable = false;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DoAction());
    }

    IEnumerator DoAction()
    {
        yield return new WaitForSeconds(beginWaitTime);
        // 생겨남
        transform.GetChild(0).gameObject.SetActive(true);
        yield return new WaitForSeconds(remainTime);
        Destroy(gameObject);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (isPushable == false)
        {
            return;
        }

        PlayerAttackRange attackRange = other.gameObject.GetComponent<PlayerAttackRange>();
        if (attackRange == null)
        {
            return;
        }
        Debug.Log("밀려남!");
        // 플레이어가 베리를 때린 상황
        // -> 베리가 플레이어의 반대 방향으로 튕겨져 나감
        Vector3 playerToBerry = transform.position - PlayerController.instance.transform.position;
        playerToBerry.y = 0;
        Vector3 endPosition = transform.position + playerToBerry.normalized * playerPushBerryDistance;
        Vector3 centerPosition = Vector3.Lerp(transform.position, endPosition, 0.5f);
        centerPosition += new Vector3(0, 4.0f, 0);


        GeometryUtility.MoveOnBezierCurve(transform.position, endPosition, centerPosition, gameObject, timeForPlayerPushBerry);
    }

    private void OnCollisionEnter(Collision collision)
    {

        
    }

    public void Land() => isPushable = true;
}
