using DungeonGeneration;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DungeonMaker : MonoBehaviour
{
    [SerializeField] List<DungeonTemplate> _dungeonTemplate;
    DungeonTemplate _actualDungeonTemplate;
    RoomTemplate[,] roomPlacement;
    int roomSizeToUnityRatio = 10;
    int roomMaxSize = 2;
    int treeDepth;
    int numberOfNodes;
    int gridSize;
    bool autoOffset = false;
    int manualOffset = 2;
    int offset;
    int floor = 0;
    bool alreadyAnIntersection = false;
    [Tooltip("1 chance sur : ")]
    [SerializeField] int probaExtraRoom = 5;
    [SerializeField] GameObject corridorPrefab;
    [SerializeField] Transform corridorsContainer;
    [SerializeField] GameObject player;
    GameObject spawn;

    private void Start()
    {
        InitDungeon();
    }

    private void InitDungeon()
    {
        _actualDungeonTemplate = Instantiate(_dungeonTemplate[floor]);
        if (autoOffset)
        {
            offset = roomMaxSize;
        }
        else
        {
            offset = manualOffset;
        }
        treeDepth = _dungeonTemplate[floor].GetTreeDepth();
        numberOfNodes = CountNodes(_dungeonTemplate[floor].rootNode);
        gridSize = treeDepth * (roomMaxSize + offset) * 2 - 1;
        Debug.Log(treeDepth + " space " + gridSize);
        if (_dungeonTemplate[floor].isRandomSeed)
        {
            int randomSeed = UnityEngine.Random.Range(0, 10000);
            _dungeonTemplate[floor].seed = randomSeed;
            UnityEngine.Random.InitState(randomSeed);
        }
        else
        {
            UnityEngine.Random.InitState(_dungeonTemplate[floor].seed);
        }
        StartCoroutine(CreateDungeon(_dungeonTemplate[floor].seed));
    }

    public void changeFloor()
    {
        floor++;
        alreadyAnIntersection = false;
        if (floor >= _dungeonTemplate.Count)
        {
            floor = _dungeonTemplate.Count - 1;
        }
        InitDungeon();
    }

    private IEnumerator CreateDungeon(int seed)
    {
        roomPlacement = new RoomTemplate[gridSize, gridSize];
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in corridorsContainer)
        {
            Destroy(child.gameObject);
        }
        yield return null;

        SetUpLayout(_actualDungeonTemplate.rootNode, new Vector2(gridSize / 2, gridSize / 2), null, Vector2Int.zero, Vector3.zero);

        if (transform.childCount >= numberOfNodes)
        {
            Vector3 posSpawn = new Vector3(spawn.transform.position.x, spawn.transform.position.y + 1, spawn.transform.position.z);
            //player.transform.position = posSpawn;
            Instantiate(player, posSpawn, Quaternion.identity);
        }
    }

    private void SetUpLayout(RoomSequenceNode parentRoom, Vector2 startRoomCoords, PairDoorData? exitDoorData, Vector2 prevRoomSize, Vector3 prevRoomPos)
    {
        DoorDirection? exitDoor;
        if (exitDoorData != null)
        {
            exitDoor = exitDoorData._activeDoor.GetDirection();
        } else
        {
            exitDoor = null;
        }        

        if (parentRoom.type == RoomType.Random && parentRoom.children.Count <= 1 && !alreadyAnIntersection)
        {
            int prob = UnityEngine.Random.Range(0, probaExtraRoom);
            if (prob == 0)
            {
                parentRoom.type = RoomType.Intersection;
                alreadyAnIntersection = true;
            }
            else
            {
                parentRoom.type = RoomType.Enemy;
            }
        } 
        if (parentRoom.type == RoomType.Random && alreadyAnIntersection)
        {
            parentRoom.type = RoomType.Enemy;
        }
        if (parentRoom.type == RoomType.Intersection && parentRoom.children.Count <= 1)
        {
            parentRoom.children.Add(new ChildConnection { child = new RoomSequenceNode(RoomType.Treasure) });
        }
        List<RoomTemplate> allRoomOfType = _actualDungeonTemplate.GetRoomsOfType(parentRoom.type); //get all room from a specific type (ennemy, boss etc...)
        RoomTemplate newRoom = null;
        newRoom = allRoomOfType[UnityEngine.Random.Range(0, allRoomOfType.Count)]; // get random room from type

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
            StartCoroutine(CreateDungeon(_dungeonTemplate[floor].seed + 1));
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
        GameObject newRoomInstance = Instantiate(newRoom.roomPrefabs, worldPos, Quaternion.identity, transform);
        if (parentRoom.type == RoomType.Hub || parentRoom.type == RoomType.Save)
        {
            spawn = newRoomInstance;
        }

        if (exitDoorData != null )
        {
            BuildCorridors(prevRoomPos + exitDoorData._activeDoor.gameObject.transform.position, newRoomInstance.transform.position + entryDoorData._activeDoor.gameObject.transform.position);
        }

        for (int i = 0; i < exitDoors.Count; i++)
        {
            SetUpLayout(parentRoom.children[i].child, newCoords, exitDoors[i], newRoom.roomSize, newRoomInstance.transform.position);
        }
    }

    public void BuildCorridors(Vector3 fromPos, Vector3 toPos)
    {
        Vector2 offset = new Vector2();

        if (fromPos.x < toPos.x)
        {
            offset.x = -0.5f;
        } else {
            offset.x = 0.5f;
        }

        if (fromPos.z < toPos.z)
        {
            offset.y = -0.5f;
        } else {
            offset.y = 0.5f;
        }

        if (Mathf.Approximately(fromPos.z, toPos.z))
        {
            // Couloir purement horizontal
            for (float x = Mathf.Min(fromPos.x, toPos.x) + 1; x <= Mathf.Max(fromPos.x, toPos.x) - 1; x += 1)
            {
                Instantiate(corridorPrefab, new Vector3(x, fromPos.y, fromPos.z), Quaternion.identity, corridorsContainer);
            }
        }
        else if (Mathf.Approximately(fromPos.x, toPos.x))
        {
            // Couloir purement vertical
            for (float z = Mathf.Min(fromPos.z, toPos.z) + 1; z <= Mathf.Max(fromPos.z, toPos.z) - 1; z += 1)
            {
                Instantiate(corridorPrefab, new Vector3(fromPos.x, fromPos.y, z), Quaternion.identity, corridorsContainer);
            }
        }
        else
        {
            if (Math.Abs(fromPos.x - toPos.x) > Math.Abs(fromPos.z - toPos.z)) // si horizontal > vertical
            {
                float midX = Mathf.Round((fromPos.x + toPos.x) / 2f) + offset.x; // offset appliqué une seule fois ici

                for (float x = Mathf.Min(fromPos.x, midX) + 1; x <= Mathf.Max(fromPos.x, midX) - 1; x += 1) // horizontal 1
                {
                    Instantiate(corridorPrefab, new Vector3(x, fromPos.y, fromPos.z), Quaternion.identity, corridorsContainer);
                }

                Instantiate(corridorPrefab, new Vector3(midX, fromPos.y, fromPos.z), Quaternion.identity, corridorsContainer); // corner 1

                for (float z = Mathf.Min(fromPos.z, toPos.z) + 1; z <= Mathf.Max(fromPos.z, toPos.z) - 1; z += 1) // vertical
                {
                    Instantiate(corridorPrefab, new Vector3(midX, fromPos.y, z), Quaternion.identity, corridorsContainer);
                }

                Instantiate(corridorPrefab, new Vector3(midX, fromPos.y, toPos.z), Quaternion.identity, corridorsContainer); // corner 2

                for (float x = Mathf.Min(midX, toPos.x) + 1; x <= Mathf.Max(midX, toPos.x) - 1; x += 1) // horizontal 2
                {
                    Instantiate(corridorPrefab, new Vector3(x, fromPos.y, toPos.z), Quaternion.identity, corridorsContainer);
                }
            }
            else // si vertical > horizontal
            {
                float midZ = Mathf.Round((fromPos.z + toPos.z) / 2f) + offset.y; // offset appliqué une seule fois ici

                for (float z = Mathf.Min(fromPos.z, midZ) + 1; z <= Mathf.Max(fromPos.z, midZ) - 1; z += 1) // vertical 1
                {
                    Instantiate(corridorPrefab, new Vector3(fromPos.x, fromPos.y, z), Quaternion.identity, corridorsContainer);
                }

                Instantiate(corridorPrefab, new Vector3(fromPos.x, fromPos.y, midZ), Quaternion.identity, corridorsContainer); // corner 1

                for (float x = Mathf.Min(fromPos.x, toPos.x) + 1; x <= Mathf.Max(fromPos.x, toPos.x) - 1; x += 1) // horizontal
                {
                    Instantiate(corridorPrefab, new Vector3(x, fromPos.y, midZ), Quaternion.identity, corridorsContainer);
                }

                Instantiate(corridorPrefab, new Vector3(toPos.x, fromPos.y, midZ), Quaternion.identity, corridorsContainer); // corner 2

                for (float z = Mathf.Min(midZ, toPos.z) + 1; z <= Mathf.Max(midZ, toPos.z) - 1; z += 1) // vertical 2
                {
                    Instantiate(corridorPrefab, new Vector3(toPos.x, fromPos.y, z), Quaternion.identity, corridorsContainer);
                }
            }
        }
    }

    private int CountNodes(RoomSequenceNode node)
    {
        if (node == null)
            return 0;

        int count = 1;

        foreach (var childConnection in node.children)
        {
            count += CountNodes(childConnection.child);
        }

        return count;
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
                    Debug.Log("outOfRange");
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
            entryDoorData = entryPool.doors[UnityEngine.Random.Range(0, entryPool.doors.Count)];
            unusedPools.RemoveAll(pool => pool.type == entrySide);
            doorsManager.SetActiveDoors(entryDoorData);
            doorsManager._entryDoor = entryDoorData;
        }

        int doorCount = Mathf.Min(numberOfDoors, unusedPools.Count);

        for (int i = 0; i < doorCount; i++) // for each child of the new room, get an exit door
        {
            DoorTypePool randomPool = unusedPools[UnityEngine.Random.Range(0, unusedPools.Count)];
            PairDoorData randomDoor = randomPool.doors[UnityEngine.Random.Range(0, randomPool.doors.Count)];
            usedDoors.Add(randomDoor);
            unusedPools.Remove(randomPool);
        }

        foreach (PairDoorData exitDoor in usedDoors) // set active all exit door
        {
            doorsManager.SetActiveDoors(exitDoor);
        }

        doorsManager._exitDoor = usedDoors.ToArray();

        return (usedDoors, entryDoorData); // return all exit door and the only entry door
    }
}





// a mettre sur le playe quand on aura le systeme de deplacement
//private void OnTriggerEnter(Collider other)
//{
//    other.gameObject.GetComponent<DoorsManager>().OpenEntryDoor();
//    other.gameObject.GetComponent<DoorsManager>().OpenExitDoor(); // temporaire en attendant la detection d'ennemis
//}