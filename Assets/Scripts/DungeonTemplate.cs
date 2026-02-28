using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration
{
    [CreateAssetMenu(fileName = "NewDungeonTemplate", menuName = "DungeonGeneration/DungeonTemplate")]
    public class DungeonTemplate : ScriptableObject
    {
        [Header("Seed")]
        public int seed = 0;

        [Header("Seed")]
        public bool isRandomSeed = true;

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

        public int GetTreeDepth()
        {
            return GetDepthRecursive(rootNode);
        }

        private int GetDepthRecursive(RoomSequenceNode node)
        {
            if (node == null || node.children.Count == 0) return 1;

            int maxChildDepth = 0;
            foreach (var connection in node.children)
            {
                int childDepth = GetDepthRecursive(connection.child);
                if (childDepth > maxChildDepth)
                    maxChildDepth = childDepth;
            }

            return 1 + maxChildDepth;
        }
    }

    [Serializable]
    public class RoomSequenceNode
    {
        public RoomSequenceNode(RoomType _type) 
        {
            type = _type;
            children = new List<ChildConnection>();
        }
        public RoomSequenceNode() 
        {
            type = RoomType.Enemy;
            children = new List<ChildConnection>();
        }

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
