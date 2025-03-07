using System.Collections.Generic;
using Enemies;
using Grid;
using Grid.Blocks;
using Unity.Netcode;
using UnityEngine;
using Type = Grid.Type;

namespace Managers
{
    public class EnemySpawnerManager : NetworkBehaviour
    {
        public static EnemySpawnerManager Instance {private set; get; }
        public static int TotalRounds;
        public static int timeBetweenSpawns;
        private int _timeSinceSpawns;
        
        private List<SpawnerBlock> _spawners;

        private void Awake()
        {
            Instance = this;
        }
        private void Start()
        {
            if (IsServer)
            {
                
            }
        }

        public void SetSpawners(List<SpawnerBlock> spawners)
        {
            this._spawners = spawners;
        }

        public void StartMathSpawners(int turn)
        {
            _timeSinceSpawns = timeBetweenSpawns;
            foreach (var spawner in _spawners)
            {
                spawner.CalculateSpawnRate(turn);
            } 
        }
        public bool Spawn(int turn)
        {
            if (turn <= 0) return false;
            if (!IsTimeToSpawn()) return false;
            _timeSinceSpawns = 0;

            bool hasSpawned = false;
            foreach (var spawner in _spawners)
            {
                GameObject enemyToSpawn = spawner.GetEnemyToSpawn();
                if (enemyToSpawn == null)
                    continue;
                GameObject enemySpawned = Instantiate(enemyToSpawn, spawner.positionToSpawn);
                enemySpawned.GetComponent<NetworkObject>().Spawn(true);
                enemySpawned.GetComponent<Enemy>().Initialize(spawner.positionToSpawn);
                hasSpawned = true;
            }

            return hasSpawned;
        }

        private bool IsTimeToSpawn()
        {
            return _timeSinceSpawns++ >= timeBetweenSpawns;
        }
    }
}