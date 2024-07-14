using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     기하학 함수 쪼가리들. 코드 복붙을 위해 재사용성을 높입니다.
/// </summary>
public class GeometryUtility : MonoBehaviour
{
    /// <summary>
    ///     베지어 곡선을 따라 대상이 움직이는 함수
    /// </summary>
    /// <param name="moveObject">움직일 대상입니다.</param>
    /// <returns></returns>
    /// <remarks>참조 링크 : https://leekangw.github.io/posts/49/</remarks>
    static public IEnumerator MoveOnBezierCurve(Vector3 start, Vector3 end, Vector3 center, GameObject moveObject, float duration)
    {
        float time = 0;

        while (time < duration)
        {
            // 부동소수점 오차도 문제 때문에 time += Time.deltaTime / timeForBerryMove 로직을 쓰지 않음
            float lerpValue = time / duration;
            Vector2 pointStartToCenter = Vector3.Lerp(start, center, lerpValue);
            Vector2 pointCenterToEnd = Vector3.Lerp(center, end, lerpValue);
            moveObject.transform.position = Vector3.Lerp(pointStartToCenter, pointCenterToEnd, lerpValue);

            time += Time.deltaTime;
            yield return null;
        }

        moveObject.transform.position = end; // 베리가 도착 지점에 정확히 위치함을 보장한다.
    }
}
