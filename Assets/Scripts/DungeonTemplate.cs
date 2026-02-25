using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration
{
    /// <summary>
    /// ScriptableObject définissant la structure d'un étage de donjon.
    /// Créer via : Assets > Create > DungeonGeneration > DungeonTemplate
    /// </summary>
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

        /// <summary>
        /// Retourne toutes les RoomTemplates disponibles pour un type donné.
        /// </summary>
        public List<RoomTemplate> GetRoomsOfType(RoomType type)
        {
            foreach (var pool in roomPools)
                if (pool.type == type)
                    return pool.rooms;
            return new List<RoomTemplate>();
        }
    }

    // ─────────────────────────────────────────────
    // Nœud de séquence (structure arborescente)
    // ─────────────────────────────────────────────

    [Serializable]
    public class RoomSequenceNode
    {
        [Tooltip("Type de salle à générer pour ce nœud")]
        public RoomType type = RoomType.Enemy;

        //[Tooltip("Ce nœud fait partie du chemin critique (obligatoire pour progresser)")]
        //public bool isCriticalPath = true;

        [SerializeReference]
        [Tooltip("Connexions vers les nœuds enfants")]
        public List<ChildConnection> children = new List<ChildConnection>();
    }

    [Serializable]
    public class ChildConnection
    {
        [SerializeReference]
        public RoomSequenceNode child = new RoomSequenceNode(); // valeur par défaut !

        //[Tooltip("Direction préférée de la connexion (suggestion, pas forcée)")]
        //public Direction preferredDirection = Direction.South;

        //[Tooltip("Ce lien fait partie du chemin critique")]
        //public bool isCriticalLink = true;
    }

    // ─────────────────────────────────────────────
    // Pool de rooms par type
    // ─────────────────────────────────────────────

    [Serializable]
    public class RoomTypePool
    {
        public RoomType type;
        public List<RoomTemplate> rooms = new List<RoomTemplate>();
    }
}
