using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Data.Common;
using System.Linq;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    public List<FloorEventEntry> floorEventEntries = new List<FloorEventEntry>();

    public static int floorNumber = 0;

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
}
