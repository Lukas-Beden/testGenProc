using DungeonGeneration;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class DungeonMaker : MonoBehaviour
{
    [SerializeField] DungeonTemplate _dungeonTemplate;
    Dictionary<Vector2, RoomTemplate> _mapGrid = new();
    RoomTemplate[,] roomPlacement;
    int roomSizeToUnityRatio = 10;
    int roomMaxSize = 2;
    int treeDepth;
    int gridSize;
    bool autoOffset = false;
    int manualOffset = 2;
    int offset;


    private void Start()
    {
        _dungeonTemplate = Instantiate(_dungeonTemplate);
        if (autoOffset)
        {
            offset = roomMaxSize;
        } else
        {
            offset = manualOffset;
        }
        //Random.InitState(_dungeonTemplate.seed);
        treeDepth = _dungeonTemplate.GetTreeDepth();
        gridSize = treeDepth * (roomMaxSize + offset) * 2 - 1;
        Debug.Log(treeDepth + " space " + gridSize);
        if (_dungeonTemplate.isRandomSeed)
        {
            StartCoroutine(CreateDungeon(Random.Range(0, 10000)));
        } else
        {
            StartCoroutine(CreateDungeon(_dungeonTemplate.seed));
        }
        
        
    }

    private IEnumerator CreateDungeon(int seed)
    {
        _dungeonTemplate.seed = seed;

        InitGrid(gridSize, gridSize); //clear previous dungeon
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        yield return null;

        SetUpLayout(_dungeonTemplate.rootNode, new Vector2(gridSize / 2, gridSize / 2), null, Vector2Int.zero);
    }

    private void SetUpLayout(RoomSequenceNode parentRoom, Vector2 startRoomCoords, DoorDirection? exitDoor, Vector2 prevRoomSize)
    {
        if (parentRoom.type == RoomType.Intersection && parentRoom.children.Count <= 1)
        {
            parentRoom.children.Add(new ChildConnection { child = new RoomSequenceNode(RoomType.Tresor) });
        }
        List<RoomTemplate> allRoomOfType = _dungeonTemplate.GetRoomsOfType(parentRoom.type); //get all room from a specific type (ennemy, boss etc...)
        RoomTemplate newRoom = null;
        newRoom = allRoomOfType[Random.Range(0, allRoomOfType.Count)]; // get random room from type

        Vector2 newCoords = new();
        switch (exitDoor)
        {
            case DoorDirection.North:
                newCoords = new Vector2(startRoomCoords.x, startRoomCoords.y + prevRoomSize.y + offset);
                break;
            case DoorDirection.East:
                newCoords = new Vector2(startRoomCoords.x + prevRoomSize.x + offset, startRoomCoords.y);
                break;
            case DoorDirection.South:
                newCoords = new Vector2(startRoomCoords.x, startRoomCoords.y - newRoom.roomSize.y - offset);
                break;
            case DoorDirection.West:
                newCoords = new Vector2(startRoomCoords.x - newRoom.roomSize.x - offset, startRoomCoords.y);
                break;
        }

        if (!TryPlaceRoom(newRoom, newCoords)) // if dungeon can't be built
        {
            if (_dungeonTemplate.isRandomSeed)
            {
                StartCoroutine(CreateDungeon(Random.Range(0, 10000)));
            }
            else
            {
                StartCoroutine(CreateDungeon(_dungeonTemplate.seed+1));
            }
            return;
        }

        newRoom.roomPrefabs.GetComponent<DoorsManager>().SetStartDoors(); // reset doors on prefab
        var (exitDoors, entryDoorData) = GetExitDoor(newRoom, parentRoom.children.Count, exitDoor); // get entry door and exit door for the new room

        Vector2 gridCenter = new Vector2(gridSize / 2f, gridSize / 2f);
        Vector3 worldPos = new Vector3(
            (newCoords.x - gridCenter.x) * roomSizeToUnityRatio,
            0,
            (newCoords.y - gridCenter.y) * roomSizeToUnityRatio
        );
        Instantiate(newRoom.roomPrefabs, worldPos, Quaternion.identity, transform);

        for (int i = 0; i < exitDoors.Count; i++)
        {
            SetUpLayout(parentRoom.children[i].child, newCoords, exitDoors[i]._activeDoor.GetDirection(), newRoom.roomSize);
        }
    }

    private void InitGrid(int width, int height)
    {
        roomPlacement = new RoomTemplate[width, height];
    }

    private bool TryPlaceRoom(RoomTemplate room, Vector2 startCoords)
    {
        int startX = (int)startCoords.x;
        int startY = (int)startCoords.y;

        for (int i = 0; i < room.roomSize.x; i++)
        {
            for (int j = 0; j < room.roomSize.y; j++)
            {
                int x = startX + i;
                int y = startY + j;

                if (x < 0 || y < 0 || x >= roomPlacement.GetLength(0) || y >= roomPlacement.GetLength(1))
                {
                    Debug.Log($"outOfRange");
                    return false;
                }

                if (roomPlacement[x, y] != null)
                {
                    Debug.Log("dejaUneSalle");
                    return false;
                }
            }
        }

        for (int i = 0; i < room.roomSize.x; i++)
        {
            for (int j = 0; j < room.roomSize.y; j++)
            {
                roomPlacement[startX + i, startY + j] = room;
            }
        }

        return true;
    }

    private (List<PairDoorData> exitDoors, PairDoorData entryDoorData) GetExitDoor(RoomTemplate room, int numberOfDoors, DoorDirection? entryDoor)
    {
        DoorDirection? entrySide; // invert the previous exit to get the new entrance 
        switch (entryDoor)
        {
            case DoorDirection.North:
                entrySide = DoorDirection.South;
                break;
            case DoorDirection.East:
                entrySide = DoorDirection.West;
                break;
            case DoorDirection.South:
                entrySide = DoorDirection.North;
                break;
            case DoorDirection.West:
                entrySide = DoorDirection.East;
                break;
            default:
                entrySide = null;
                break;
        }

        DoorsManager doorsManager = room.roomPrefabs.GetComponent<DoorsManager>();
        List<DoorTypePool> unusedPools = doorsManager.GetAllDoors();
        List<PairDoorData> usedDoors = new();
        PairDoorData entryDoorData = null;

        if (entrySide != null) // if this is not the 1st room
        {
            DoorTypePool entryPool = unusedPools.Find(pool => pool.type == entrySide); // get the entry and set it active
            entryDoorData = entryPool.doors[Random.Range(0, entryPool.doors.Count)];
            unusedPools.RemoveAll(pool => pool.type == entrySide);
            doorsManager.SetActiveDoors(entryDoorData);
        }

        int doorCount = Mathf.Min(numberOfDoors, unusedPools.Count);

        for (int i = 0; i < doorCount; i++) // for each child of the new room, get an exit door
        {
            DoorTypePool randomPool = unusedPools[Random.Range(0, unusedPools.Count)];
            PairDoorData randomDoor = randomPool.doors[Random.Range(0, randomPool.doors.Count)];
            usedDoors.Add(randomDoor);
            unusedPools.Remove(randomPool);
        }

        foreach (PairDoorData exitDoor in usedDoors) // set active all exit door
        {
            doorsManager.SetActiveDoors(exitDoor);
        }

        return (usedDoors, entryDoorData); // return all exit door and the only entry door
    }
}