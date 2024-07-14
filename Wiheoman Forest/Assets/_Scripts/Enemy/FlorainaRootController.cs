using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlorainaRootController : MonoBehaviour
{
    [SerializeField] float beginWaitTime = 0.5f;
    [SerializeField] float remainTime = 0.5f;


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
}
