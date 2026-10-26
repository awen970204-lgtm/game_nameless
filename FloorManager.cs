using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Data.Common;
using System.Linq;

// floorNumber
// finishedEventEntry0

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    public List<FloorEventEntry> floorEventEntries = new List<FloorEventEntry>();
    public List<FloorEventEntry> finishedEventEntries = new List<FloorEventEntry>();
    public FloorEventEntry pendingEventEntry;

    public static int floorNumber = 0;

    public GameObject choiceButtunPreb;
    public Transform choiceTransform;

    void Awake()
    {
        if (FloorManager.Instance == null)
            Instance = this;
        else Destroy(this.gameObject);
    }
    void Start()
    {
        if (!PlayerPrefs.HasKey($"floorNumber"))
        {
            PlayerPrefs.SetInt("floorNumber", 0);
        }
    }

    public void GameStart()
    {
        floorNumber = 0;
        pendingEventEntry = null;
        finishedEventEntries.Clear();
        NextFloor();
    }
    public void GameContinue()
    {
        floorNumber = PlayerPrefs.GetInt("floorNumber");
        if (floorEventEntries[PlayerPrefs.GetInt("pendingEventEntry")] != null)
        {
            pendingEventEntry = floorEventEntries[PlayerPrefs.GetInt("pendingEventEntry")];
        }
        foreach(var eventEntry in floorEventEntries)
        {
            if (PlayerPrefs.GetInt($"finishedEventEntry"+
                $"{floorEventEntries.IndexOf(eventEntry)}") == 1)
            {
                finishedEventEntries.Add(eventEntry);
            }
        }
    }

    public void OverFloor()
    {
        EventUI.Instance.ClosePanel();
        if (pendingEventEntry != null)
        {
            finishedEventEntries.Add(pendingEventEntry);
        }
        NextFloor();
    }

    private void NextFloor()
    {
        floorNumber += 1;
        LoadingFloor(floorNumber);
    }

    private void LoadingFloor(int nowFloor)
    {
        
    }
}
