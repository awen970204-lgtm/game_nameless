using System;
using System.Collections.Generic;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    public List<FloorEventEntry> floorEventEntries = new List<FloorEventEntry>();

    void Awake()
    {
        if (FloorManager.Instance == null)
            Instance = this;
        else Destroy(this.gameObject);
    }
}
