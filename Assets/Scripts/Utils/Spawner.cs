using System;
using System.Collections.Generic;
using Grid;
using Managers;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;
using Type = Grid.Type;

namespace Utils
{
    [Serializable]
    public class Spawner
    {
        private static float _overTheTiles = 0.5f;
        [Header("When")] [SerializeField] private TowerDefenseManager.State _timeSlot;

        [Header("IfRepeatable")] [SerializeField]
        private int _startingRound = -1;

        [SerializeField] private int _endingRound = -1;
        [SerializeField] private int _period;
        [Header("What")] [SerializeField] private GameObject _objectToSpawn;

        [Header("How")] [SerializeField] private double _spawnRate;

        [SerializeField] private List<Type> _BlockTypeToSpawnOn;
        private GridHelper _helper;
        private bool _isServer;
        private Vector2Int _position;
        private int _positionInList;
        private Random _rand = new();
        private Func<bool> Predicate;

        private int timeToRepeate;
        public GameObject ObjectToSpawn => _objectToSpawn;

        public void Initialize(bool isServer, int positionInList)
        {
            _positionInList = positionInList;
            _isServer = isServer;
            _position = new Vector2Int();
            _helper = new SpawnerGridHelper(_position, _BlockTypeToSpawnOn);
            timeToRepeate = _period;
            Predicate = CreatePredicate();
        }

        /// <summary>
        ///     Permet de creer un predicat pour la repetition si le _startingRound est different de -1.
        /// </summary>
        /// <returns></returns>
        private Func<bool> CreatePredicate()
        {
            if (_startingRound == -1) return () => true;

            if (_endingRound == -1)
                return () =>
                {
                    if (timeToRepeate == 0)
                    {
                        timeToRepeate = _period;
                        return _startingRound <= TowerDefenseManager.Instance.currentRoundNumber;
                    }

                    timeToRepeate--;
                    return false;
                };

            return () =>
            {
                if (timeToRepeate == 0)
                {
                    timeToRepeate = _period;
                    var currentRound = TowerDefenseManager.Instance.currentRoundNumber;
                    return _startingRound <= currentRound && _endingRound >= currentRound;
                }

                timeToRepeate--;
                return false;
            };
        }

        /// <summary>
        ///     Generates the position where to put the gameObjects
        /// </summary>
        /// <returns> List of positions</returns>
        private List<Vector2Int> GeneratePositions()
        {
            List<Vector2Int> listOfPosition = new();
            var i = 0;
            do
            {
                _position.x = i;
                var j = 0;
                do
                {
                    _position.y = j;
                    _helper.SetHelperPosition(_position);
                    if (_helper.IsValidCell(_position) && RandomBool()) listOfPosition.Add(_position);
                    j++;
                } while (j < TilingGrid.Size);

                i++;
            } while (i < TilingGrid.Size);

            return listOfPosition;
        }

        private bool RandomBool()
        {
            return _rand.NextDouble() > 1 - _spawnRate;
        }

        /// <summary>
        ///     Instantiate/spawn the gameobject at the different positions
        /// </summary>
        /// <param name="listOfPosition"> Positions where to put the gameobjects</param>
        /// <param name="objectToSpawn"> GameObject to Spawn at the positions</param>
        public static void InstantiateObstacles(List<Vector2Int> listOfPosition, GameObject objectToSpawn)
        {
            foreach (var position in listOfPosition) InstantiateObstacle(position, objectToSpawn);
        }

        private static void InstantiateObstacle(Vector2Int position, GameObject objectToSpawn)
        {
            var position3d = TilingGrid.GridPositionToLocal(position);
            position3d.y += _overTheTiles;
            Object.Instantiate(objectToSpawn, position3d, Quaternion.identity);
        }

        public void AddSelfToTimeSlot(object sender,
            TowerDefenseManager.OnCurrentStateChangedEventArgs changedEventArgs)
        {
            if (changedEventArgs.newValue == _timeSlot)
                if (Predicate.Invoke())
                    if (_isServer)
                    {
                        var positions = GeneratePositions();
                        if (SpawnersManager.Instance == null)
                            throw new Exception("SpawnersManager instance has not been set !");
                        SpawnersManager.Instance.PlaceObjectsClientRpc(positions.ToArray(), _positionInList);
                    }
        }
    }
}