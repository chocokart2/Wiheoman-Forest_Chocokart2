using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractiveImageJamBottle : InteractiveImageDragAndMove
{
    public static InteractiveImageJamBottle instance;
    private bool isJamPicked = false;
    [SerializeField] private GameObject jamBottleGameObject;
    [SerializeField] private GameObject jamBlobGameObject;
    [SerializeField] private GameObject jamTraceGameObject;
    [SerializeField] private GameObject jamTraceTransformParentGameObject;
    [SerializeField] private GameObject plainBreadGameObject;
    [SerializeField] private Sprite imageFullJam;
    [SerializeField] private Sprite imageHalfJam;
    [SerializeField] private float jamDragTerm;
    [SerializeField] private float jamTraceSize;
    private RectTransform plainBreadRect;
    private UnityEngine.UI.Image jamBottleImageComponent;
    private Vector3 lastDragPosition;
    private bool[,] m_isJamPlaced;
    private float m_xStart;
    private float m_yStart;
    private float m_xTerm;
    private float m_yTerm;

    public bool IsJamPicked()
    {
        return isJamPicked;
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (isJamPicked == false)
        {
            isJamPicked = true;
            jamBottleImageComponent.sprite = imageHalfJam;
            jamBlobGameObject.SetActive(true);

            Vector3 m_resultPosition = new Vector3();
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvasRectTransform,
                Input.mousePosition,
                canvasComponent.worldCamera,
                out m_resultPosition);
            mouseToUiOffsetVector3 = transform.position - m_resultPosition;
            jamBlobGameObject.transform.position = GetNewFramePosition();

            Cursor.visible = false;
        }
        else
        {
            Cursor.visible = true;

            isJamPicked = false;
            jamBottleImageComponent.sprite = imageFullJam;
            jamBlobGameObject.SetActive(false);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        jamBottleImageComponent = jamBottleGameObject.GetComponent<UnityEngine.UI.Image>();

        BaseStart();
        myRectTransform = jamBlobGameObject.GetComponent<RectTransform>();
        lastDragPosition = transform.position;
        jamDragTerm *= jamDragTerm;

        m_isJamPlaced = new bool[SlimeJamSpreadingController.instance.GetJudgeCountPerSide(),
            SlimeJamSpreadingController.instance.GetJudgeCountPerSide()];
        plainBreadRect = plainBreadGameObject.GetComponent<RectTransform>();

        int judgeCountPerSide = SlimeJamSpreadingController.instance.GetJudgeCountPerSide();

        Vector2 m_referenceResolution = transform.parent.parent.GetComponent<CanvasScaler>().referenceResolution;
        Vector2 m_currentResolution = new Vector2(Screen.width, Screen.height);
        float scaleFactor = m_currentResolution.x / m_referenceResolution.x;
        m_xStart = plainBreadGameObject.transform.position.x
            - plainBreadRect.sizeDelta.x * plainBreadRect.localScale.x / 2 * scaleFactor;
        m_yStart = plainBreadGameObject.transform.position.y
            - plainBreadRect.sizeDelta.y * plainBreadRect.localScale.y / 2 * scaleFactor;

        m_xTerm = plainBreadRect.localScale.x * plainBreadRect.sizeDelta.x / judgeCountPerSide * scaleFactor;
        m_yTerm = plainBreadRect.localScale.y * plainBreadRect.sizeDelta.y / judgeCountPerSide * scaleFactor;

    }

    private void Update()
    {
        DoJamSpread();
    }

    void DoJamSpread()
    {
        if (isJamPicked)
        {
            jamBlobGameObject.transform.position = GetNewFramePosition() + mouseToUiOffsetVector3;

            if (Input.GetMouseButton(0) &&
                InteractiveImageJamPlainBread.instance.isOnBread)
            {
                //Debug.Log($">> MYJAM : {jamBlobGameObject.transform.position}");
                float posX = jamBlobGameObject.transform.position.x;
                float posY = jamBlobGameObject.transform.position.y;
                int x = -1;
                int y = -1;
                if (posX >= m_xStart)
                {
                    x = (int)((posX - m_xStart) / m_xTerm);
                }
                if (posY >= m_yStart)
                {
                    y = (int)((posY - m_yStart) / m_yTerm);
                }

                if (x < 0 || x >= SlimeJamSpreadingController.instance.GetJudgeCountPerSide()) return;
                if (y < 0 || y >= SlimeJamSpreadingController.instance.GetJudgeCountPerSide()) return;

                if (m_isJamPlaced[x, y] == false)
                {
                    m_isJamPlaced[x, y] = true;

                    GameObject one = Instantiate(jamTraceGameObject,
                        jamBlobGameObject.transform.position, jamTraceGameObject.transform.rotation);
                    one.transform.SetParent(jamTraceTransformParentGameObject.transform);
                    one.transform.localScale = new Vector3(jamTraceSize, jamTraceSize);

                    SlimeJamSpreadingController.instance.Placed();
                }
            }
        }
    }
}
