using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestShopManager : MonoBehaviour
{
    [Header("판매할 아이템")]
    [SerializeField] private TestItem[] sellItem;

    [Header("상점 ui")]
    [SerializeField] private GameObject storeUI;

    [Header("상점 판매 ui")]
    [SerializeField] private GameObject storeUIPrefab;

    [Header("상점 판매 ui가 배치될 부모 객체")]
    [SerializeField] private Transform storeUIParent;

    private bool isStoreActive = false;
    private int selectedIndex = 0;
    private GameObject selectedItemUI;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isStoreActive = !isStoreActive;
            storeUI.SetActive(isStoreActive);

            if (isStoreActive)
            {
                InitSlot();
                SelectItem(0);
            }
        }

        if (isStoreActive)
        {
            SelectInput();
        }
    }

    private void InitSlot()
    {
        foreach (Transform child in storeUIParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var item in sellItem)
        {
            GameObject newItemUI = Instantiate(storeUIPrefab, storeUIParent);

            newItemUI.transform.Find("ItemName_Text").GetComponent<TextMeshProUGUI>().text = item.ItemName;
        }
    }

    private void SelectInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            ChangeSelection(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            ChangeSelection(1);
        }
    }

    private void SelectItem(int index)
    {
        if (storeUIParent != null && storeUIParent.childCount > 0)
        {
            selectedIndex = Mathf.Clamp(index, 0, storeUIParent.childCount - 1);
            selectedItemUI = storeUIParent.GetChild(selectedIndex).gameObject;
            ToggleOutline(selectedItemUI, true);
        }
    }

    private void ChangeSelection(int direction)
    {
        selectedIndex = Mathf.Clamp(selectedIndex + direction, 0, storeUIParent.childCount - 1);

        GameObject newItemUI = storeUIParent.GetChild(selectedIndex).gameObject;

        if (selectedItemUI != null)
        {
            ToggleOutline(selectedItemUI, false);
        }

        ToggleOutline(newItemUI, true);
        selectedItemUI = newItemUI;

        TestItem selectedItem = sellItem[selectedIndex];
        Transform backgroundPanel = storeUI.transform.Find("Store_BackGroundPanel");
        Transform itemImagePanel = backgroundPanel.transform.Find("Item_ImagePanel");
        Image itemImage = itemImagePanel.transform.Find("Image").GetComponent<Image>();
        itemImage.sprite = selectedItem.ItemImage;
    }

    private void ToggleOutline(GameObject itemUI, bool enable)
    {
        var outline = itemUI.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = enable;
        }
    }
}
