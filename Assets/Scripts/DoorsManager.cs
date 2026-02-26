using DungeonGeneration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DoorsManager : MonoBehaviour
{
    [SerializeField] DoorTypePool[] _doorPools;

    public void SetStartDoors()
    {
        foreach (DoorTypePool doorTypePool in _doorPools)
        {
            foreach (PairDoorData pairDoorData in doorTypePool.doors)
            {
                pairDoorData._activeDoor.gameObject.SetActive(false);
                pairDoorData._inactiveDoor.gameObject.SetActive(true);
            }
        }
    }

    public List<DoorTypePool> GetAllDoors()
    {
        return new List<DoorTypePool>(_doorPools);
    }

    public void SetActiveDoors(PairDoorData pair)
    {
        pair._activeDoor.gameObject.SetActive(true);
        pair._inactiveDoor.gameObject.SetActive(false);
    }
}

[Serializable]
public class DoorTypePool
{
    public DoorDirection type;
    public List<PairDoorData> doors = new List<PairDoorData>();
}

[Serializable]
public class PairDoorData
{
    public DoorData _activeDoor;
    public DoorData _inactiveDoor;
}