using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotbarSlotUI : MonoBehaviour
{
    public static readonly int FULLSTACK_COUNT = 64;

    [SerializeField] private Color unselectedColour;
    [SerializeField] private Color selectedColour;

    public bool IsSelected;
    public bool HasItem;
    [field: SerializeField] public string ItemName { get; private set; }
    [field: SerializeField] public Sprite ItemSprite { get; private set; }
    [field: SerializeField] public int StackCount { get; private set; }

    private Image bgImage;
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemCountTxt;


    private void Awake() {
        bgImage = GetComponent<Image>();
    }
    private void Start() {
        UpdateItemInfo();
    }
    private void Update() {
        bgImage.color = IsSelected ? selectedColour : unselectedColour;
    }
    private void UpdateItemInfo() {
        itemImage.sprite = ItemSprite;
        itemCountTxt.text = StackCount.ToString();

        if(StackCount == 0) {
            itemImage.sprite = null;
            itemImage.color = new Color(0, 0, 0, 0);
            HasItem = false;
            ItemName = string.Empty;
            itemCountTxt.gameObject.SetActive(false);
        } else {
            itemImage.color = Color.white;
            HasItem = true;
            itemCountTxt.gameObject.SetActive(true);
        }
    }
    public void SetItem(string itemName, Sprite itemSprite, int stackCount) {
        ItemName = itemName;
        ItemSprite = itemSprite;
        itemImage.sprite = itemSprite;
        StackCount = stackCount;
        itemCountTxt.text = stackCount.ToString();
        UpdateItemInfo();
    }
    public void AddItemToStack() {
        StackCount++;
        UpdateItemInfo();
    }
    public void RemoveItemFromStack() {
        StackCount--;
        UpdateItemInfo();
    }
}
