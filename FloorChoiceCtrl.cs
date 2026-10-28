using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Data.Common;
using System.Data;
using System;

public class FloorChoiceCtrl : MonoBehaviour
{
    [HideInInspector] public FloorEventEntry targetEvent;
    [SerializeField] private Image eventImage;
    [SerializeField] private List<Sprite> eventSprites = new List<Sprite>();
    [SerializeField] private TMP_Text eventName;
    [SerializeField] private Button eventButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void SetEvent(FloorEventEntry eventEntry)
    {
        if (eventEntry == null) return;

        targetEvent = eventEntry;
        switch (eventEntry.category)
        {
            case FloorEventCategory.NonCombat:
                eventName.text = "空閒";
                break;
            case FloorEventCategory.NormalBattle:
                eventName.text = "一般戰鬥";
                break;
            case FloorEventCategory.HardBattle:
                eventName.text = "菁英戰鬥";
                break;
            case FloorEventCategory.Boss:
                eventName.text = "首領戰鬥";
                break;
            case FloorEventCategory.Rest:
                eventName.text = "休整";
                break;
            case FloorEventCategory.Shop:
                eventName.text = "商店";
                break;
            default :
                eventName.text = "未包含";
                Debug.Log("floor button error");
                break;
        }
        int n = Array.IndexOf(Enum.GetValues(typeof(FloorEventCategory)), targetEvent.category);
        if (eventSprites.Count > n)
        {
            eventImage.sprite = eventSprites[n];
        }

        eventButton.onClick.AddListener(
            ()=> FloorManager.Instance.CompletedSelectionEvent(eventEntry));
    }
}
