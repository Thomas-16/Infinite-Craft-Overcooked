using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LLement : PickupableObject
{
    [field: SerializeField]
    public string ElementName { get; private set; }

    [SerializeField] private TextMeshPro text1;
    [SerializeField] private TextMeshPro text2;
    [SerializeField] private TextMeshPro text3;
    [SerializeField] private TextMeshPro text4;
    [SerializeField] private TextMeshPro text5;
    [SerializeField] private TextMeshPro text6;

    private void Awake() {
        SetElementName(ElementName);
    }

    public void SetElementName(string elementName) {
        ElementName = elementName;
        text1.text = ElementName;
        text2.text = ElementName;
        text3.text = ElementName;
        text4.text = ElementName;
        text5.text = ElementName;
        text6.text = ElementName;
    }
}
