using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlimeJamSpreadingController : MonoBehaviour
{
    public static SlimeJamSpreadingController instance;
    [SerializeField] GameObject plainBreadGameObject;
    [SerializeField] GameObject judgeGameObject;
    [SerializeField] int numberOfJudgePiecesPerSide;
    [SerializeField] float jamSize;
    bool isJamPicked;
    Vector3 prevPos; 
    int jamJudgeMax = 0;
    int count = 0;

    public void Add()
    {
        jamJudgeMax++;
    }
    
    public void Placed()
    {
        count++;
        End();
    }
    
    public void End()
    {
        float spreadRatio = (float)count / (float)jamJudgeMax;

        EFoodRank result;
        if (spreadRatio > 0.9f)
        {
            result = EFoodRank.Good;
        }
        else if (spreadRatio > 0.5f)
        {
            result = EFoodRank.Standard;
        }
        else
        {
            result = EFoodRank.Bad;
        }

        Debug.Log($"food result = {result}");
    }

    public int GetJudgeCountPerSide()
    {
        return numberOfJudgePiecesPerSide;
    }

    [System.Obsolete]
    private void BakeJudgePieces()
    {
        RectTransform m_placeRectTransform = plainBreadGameObject.GetComponent<RectTransform>();

        //float offsetX = - m_placeRectTransform.sizeDelta.x / 2.0f * m_placeRectTransform.localScale.x;
        Vector2 m_referenceResolution = transform.parent.GetComponent<CanvasScaler>().referenceResolution;
        Vector2 m_currentResolution = new Vector2(Screen.width, Screen.height);
        Vector2 scaleFactor = new Vector2(
            m_currentResolution.x / m_referenceResolution.x,
            m_currentResolution.y / m_referenceResolution.y);

        float offsetY = - m_placeRectTransform.sizeDelta.y / 2.0f * m_placeRectTransform.localScale.y;
        float scaleX = m_placeRectTransform.localScale.x / (float)numberOfJudgePiecesPerSide;
        float scaleY = m_placeRectTransform.localScale.y / (float)numberOfJudgePiecesPerSide;
        float termX = m_placeRectTransform.localScale.x * m_placeRectTransform.sizeDelta.x / (float)numberOfJudgePiecesPerSide;
        float termY = m_placeRectTransform.localScale.y * m_placeRectTransform.sizeDelta.y / (float)numberOfJudgePiecesPerSide;
        float offsetX = scaleX / 2;

        float breadLeftX = - m_placeRectTransform.sizeDelta.x * m_placeRectTransform.localScale.x / 2;
        float breadBottomY = - m_placeRectTransform.sizeDelta.y * m_placeRectTransform.localScale.y / 2;

        for (int x = 0; x < numberOfJudgePiecesPerSide; ++x)
        {
            for (int y = 0; y < numberOfJudgePiecesPerSide; ++y)
            {


                GameObject instantiatedObject = Instantiate(judgeGameObject, transform);
                RectTransform m_instantiatedObjectRectTransform = instantiatedObject.GetComponent<RectTransform>();

                instantiatedObject.transform.localScale =
                    new Vector3(Mathf.Max(jamSize, scaleX), Mathf.Max(jamSize, scaleY), 1);


                m_instantiatedObjectRectTransform.position =
                    plainBreadGameObject.transform.position +
                    new Vector3(
                        (m_instantiatedObjectRectTransform.sizeDelta.x * scaleX / 2
                        + breadLeftX
                        + x * termX) * scaleFactor.x,
                        (m_instantiatedObjectRectTransform.sizeDelta.y * scaleY / 2
                        + breadBottomY + y * termY) * scaleFactor.x);
            }
        }



    }

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {

        if (numberOfJudgePiecesPerSide <= 0)
        {
            numberOfJudgePiecesPerSide = 2;
        }
        if (jamSize < 0.0f)
        {
            jamSize = 0.3f;
        }

        jamJudgeMax = numberOfJudgePiecesPerSide * numberOfJudgePiecesPerSide;

    }

    // Update is called once per frame
    void Update()
    {

    }
}
