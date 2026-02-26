using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration
{
    [CreateAssetMenu(fileName = "NewDungeonTemplate", menuName = "DungeonGeneration/DungeonTemplate")]
    public class DungeonTemplate : ScriptableObject
    {
        [Header("Seed")]
        [Tooltip("0 = aléatoire à chaque génération")]
        public int seed = 0;

        [Header("Paramètres de génération")]
        [Tooltip("Nombre max de tentatives si le layout est impossible")]
        public int maxRetries = 50;

        [SerializeReference]
        [Header("Structure du donjon")]
        public RoomSequenceNode rootNode;

        [Header("Pool de RoomTemplates par type")]
        public RoomTypePool[] roomPools;

        public List<RoomTemplate> GetRoomsOfType(RoomType type)
        {
            foreach (var pool in roomPools)
                if (pool.type == type)
                    return pool.rooms;
            return new List<RoomTemplate>();
        }
    }

    [Serializable]
    public class RoomSequenceNode
    {
        [Tooltip("Type de salle à générer pour ce nœud")]
        public RoomType type = RoomType.Enemy;

        [SerializeReference]
        [Tooltip("Connexions vers les nœuds enfants")]
        public List<ChildConnection> children = new List<ChildConnection>();
    }

    [Serializable]
    public class ChildConnection
    {
        [SerializeReference]
        public RoomSequenceNode child = new RoomSequenceNode(); 
    }

    [Serializable]
    public class RoomTypePool
    {
        public RoomType type;
        public List<RoomTemplate> rooms = new List<RoomTemplate>();
    }
}
