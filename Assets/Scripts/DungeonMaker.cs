using DungeonGeneration;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DungeonMaker : MonoBehaviour
{
    [SerializeField] DungeonTemplate _dungeonTemplate;
    Dictionary<Vector2, RoomTemplate> _mapGrid = new();
    int _roomOffset = -2;
    int roomSizeToUnityRatio = 12;

    private void Start()
    {
        Random.InitState(_dungeonTemplate.seed);
        StartCoroutine(CreateDungeon(Random.Range(0, 10000)));
    }

    private IEnumerator CreateDungeon(int seed)
    {
        _dungeonTemplate.seed = seed;

        _mapGrid.Clear(); //clear previous dungeon
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        yield return null;

        SetUpLayout(_dungeonTemplate.rootNode, new Vector2(0, 0), null); //try new dungeon
    }

    private void SetUpLayout(RoomSequenceNode parentRoom, Vector2 startRoomCoords, DoorDirection? exitDoor)
    {
        List<RoomTemplate> allRoomOfType = _dungeonTemplate.GetRoomsOfType(parentRoom.type); //get all room from a specific type (ennemy, boss etc...)
        RoomTemplate newRoom = null;
        newRoom = allRoomOfType[Random.Range(0, allRoomOfType.Count)]; // get random room from type

        if (!TryPlaceRoom(newRoom, startRoomCoords)) // if dungeon can't be built
        {
            StartCoroutine(CreateDungeon(Random.Range(0, 10000))); // retry making dungeon
            return;
        }

        newRoom.roomPrefabs.GetComponent<DoorsManager>().SetStartDoors(); // reset doors on prefab
        var (exitDoors, entryDoorData) = GetExitDoor(newRoom, parentRoom.children.Count, exitDoor); // get entry door and exit door for the new room

        Vector3 newCoords; // setup where the room will be
        if (entryDoorData != null)
        {
            newCoords = new Vector3((startRoomCoords.x - entryDoorData._activeDoor.GetOffset().x) * roomSizeToUnityRatio, 0, (startRoomCoords.y - entryDoorData._activeDoor.GetOffset().y) * roomSizeToUnityRatio);
        } else
        {
            newCoords = new Vector3(startRoomCoords.x * roomSizeToUnityRatio, 0, startRoomCoords.y * roomSizeToUnityRatio);
        }

        Instantiate(newRoom.roomPrefabs, new Vector3(newCoords.x, 0, newCoords.y), Quaternion.identity, transform); // instantiate new room

        for (int i = 0; i < exitDoors.Count; i++) // recursive call of the function for each exit door in the new room
        {
            switch (exitDoors[i]._activeDoor.GetDirection())
            {
                case DoorDirection.North:
                    SetUpLayout(parentRoom.children[i].child, new Vector2(startRoomCoords.x, startRoomCoords.y/* + _roomOffset*/), DoorDirection.North);
                    break;
                case DoorDirection.East:
                    SetUpLayout(parentRoom.children[i].child, new Vector2(startRoomCoords.x/* + _roomOffset*/, startRoomCoords.y), DoorDirection.East);
                    break;
                case DoorDirection.South:
                    SetUpLayout(parentRoom.children[i].child, new Vector2(startRoomCoords.x, startRoomCoords.y/* - _roomOffset*/), DoorDirection.South);
                    break;
                case DoorDirection.West:
                    SetUpLayout(parentRoom.children[i].child, new Vector2(startRoomCoords.x/* - _roomOffset*/, startRoomCoords.y), DoorDirection.West);
                    break;
            }
        }
    }

    private bool TryPlaceRoom(RoomTemplate room, Vector2 startCoords)
    {
        for (int i = 0; i < room.roomSize.x; i++) // check if a room is already on these coordinates
        {
            for (int j = 0; j < room.roomSize.y; j++)
            {
                if (_mapGrid.ContainsKey(new Vector2(startCoords.x + i, startCoords.y + j)))
                {
                    return false;
                }
            }    
        }
        for (int i = 0; i < room.roomSize.x; i++) // if there is none, claim the place
        {
            for (int j = 0; j < room.roomSize.y; j++)
            {
                _mapGrid[new Vector2(startCoords.x + i, startCoords.y + j)] = room;
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