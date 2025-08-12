using System.Collections;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

[DisallowMultipleComponent]
public class CoinSpawner : NetworkBehaviour
{
    public static CoinSpawner Instance; // simple singleton for coin notifications

    [Header("Coin Prefabs (NetworkObject prefabs)")]
    [SerializeField] private NetworkObject coinPrefabSmall; // 5 points
    [SerializeField] private NetworkObject coinPrefabLarge; // 10 points

    [Header("Spawn Mode")]
    [Tooltip("If true, coins spawn at the listed spawnPoints. If false, coins spawn within the Random Range.")]
    [SerializeField] private bool useSpawnPoints = true;

    [Tooltip("Transforms used as fixed spawn points when Use Spawn Points is true.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Random Range (used when Use Spawn Points = false)")]
    [Tooltip("Y position at which coins will spawn (height).")]
    [SerializeField] private float spawnY = 1f;
    [Tooltip("Minimum X for random spawn (world coordinates)")]
    [SerializeField] private float minX = -10f;
    [Tooltip("Maximum X for random spawn (world coordinates)")]
    [SerializeField] private float maxX = 10f;
    [Tooltip("Minimum Z for random spawn (world coordinates)")]
    [SerializeField] private float minZ = -10f;
    [Tooltip("Maximum Z for random spawn (world coordinates)")]
    [SerializeField] private float maxZ = 10f;

    [Header("Spawn Settings")]
    [Tooltip("Seconds between spawn attempts (host only)")]
    [SerializeField] private float spawnIntervalSeconds = 3.0f;
    [Tooltip("Maximum number of coins allowed in the scene at once")]
    [SerializeField] private int maxConcurrentCoins = 20;

    [Header("Coin Type Probability")]
    [Range(0f, 1f)]
    [Tooltip("Probability to spawn a large coin (10 points). e.g. 0.3 = 30% large, 70% small.")]
    [SerializeField] private float largeCoinProbability = 0.3f;

    [Header("Optional (Testing)")]
    [Tooltip("If >=0, initializes UnityEngine.Random with this seed on host for reproducible spawn patterns.")]
    [SerializeField] private int randomSeed = -1;

    private int currentCoinCount = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Debug.LogWarning("Multiple CoinSpawner instances detected. Using the first one as Instance.");
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            if (randomSeed >= 0)
                Random.InitState(randomSeed);

            StartCoroutine(SpawnLoop());
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (Object != null && Object.HasStateAuthority)
        {
            yield return new WaitForSeconds(spawnIntervalSeconds);

            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.MatchState.Running)
                continue;

            if (currentCoinCount >= maxConcurrentCoins)
                continue;

            SpawnRandomCoin();
        }
    }


    private void SpawnRandomCoin()
    {
        if (!Object.HasStateAuthority) return;

        Vector3 spawnPos;
        Quaternion spawnRot = Quaternion.identity;

        if (useSpawnPoints)
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return;
            int idx = Random.Range(0, spawnPoints.Length);
            Transform sp = spawnPoints[idx];
            spawnPos = sp.position;
            spawnRot = sp.rotation;
        }
        else
        {
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            spawnPos = new Vector3(x, spawnY, z);
            spawnRot = Quaternion.identity;
        }

        // Host decides coin type
        bool spawnLarge = Random.value < largeCoinProbability;
        NetworkObject prefabToUse = spawnLarge ? coinPrefabLarge : coinPrefabSmall;

        if (prefabToUse == null)
        {
            Debug.LogWarning("CoinSpawner: coin prefab(s) not assigned.");
            return;
        }

        var spawned = Runner.Spawn(prefabToUse, spawnPos, spawnRot, null);
        if (spawned != null)
        {
            currentCoinCount++;
            //Debug.Log($"[CoinSpawner] Host spawned coin '{prefabToUse.name}' at {spawnPos}.");

            if (spawned.TryGetComponent<Coin>(out var coinComp))
            {
                coinComp.RPC_SetSpawnTransform(spawnPos, spawnRot);
            }
        }



    }

    public void NotifyCoinDespawned()
    {
        currentCoinCount = Mathf.Max(0, currentCoinCount - 1);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!useSpawnPoints)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.25f);
            Vector3 center = new Vector3((minX + maxX) * 0.5f, spawnY, (minZ + maxZ) * 0.5f);
            Vector3 size = new Vector3(Mathf.Abs(maxX - minX), 0.01f, Mathf.Abs(maxZ - minZ));
            Gizmos.DrawCube(center, size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, size);
        }
        else
        {
            if (spawnPoints != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var sp in spawnPoints)
                {
                    if (sp != null) Gizmos.DrawWireSphere(sp.position, 0.4f);
                }
            }
        }
    }
#endif
}
