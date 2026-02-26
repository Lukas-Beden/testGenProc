using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRoomTemplate", menuName = "DungeonGeneration/RoomTemplate")]
public class RoomTemplate : ScriptableObject
{
    [SerializeField] RoomType roomType;
    [SerializeField] public GameObject roomPrefabs;
    [SerializeField] public Vector2 roomSize;
}
