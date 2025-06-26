using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class WaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class TrackConfig
    {
        public Transform[] spawnPoints;
        public string waypointTag;
        public string altWaypointTag;
        public Wave[] waves;           
        [HideInInspector] public Transform[] waypoints;    
        [HideInInspector] public Transform[] altWaypoints; 
    }

    [System.Serializable]
    public class Wave
    {
        public GameObject mobPrefab;
        public int mobCount;
        public float spawnRate;
    }

    public TrackConfig[] tracks; 
    public float timeBetweenWaves = 2f;
    public bool loopWaves = false;

    [Header("UI")]
    public TMP_Text waveCounterText;

    private int[] currentWaveIndices;
    private bool[] isSpawning;
    private List<GameObject>[] activeMobsPerTrack;
    private int[] lastSpawnPointIndices;

    void Start()
    {
        InitializeTracks();
        StartAllTrackSpawners();
    }

    private void InitializeTracks()
    {
        currentWaveIndices = new int[tracks.Length];
        isSpawning = new bool[tracks.Length];
        activeMobsPerTrack = new List<GameObject>[tracks.Length];
        lastSpawnPointIndices = new int[tracks.Length];

        for (int i = 0; i < tracks.Length; i++)
        {
            tracks[i].waypoints = GetSortedWaypoints(tracks[i].waypointTag);
            tracks[i].altWaypoints = GetSortedWaypoints(tracks[i].altWaypointTag);

            activeMobsPerTrack[i] = new List<GameObject>();
            lastSpawnPointIndices[i] = -1;
        }
    }

    private Transform[] GetSortedWaypoints(string tag)
    {
        return GameObject.FindGameObjectsWithTag(tag)
            .Where(go => go != null)
            .OrderBy(go => go.name)
            .Select(go => go.transform)
            .ToArray();
    }

    private void StartAllTrackSpawners()
    {
        for (int i = 0; i < tracks.Length; i++)
        {
            StartCoroutine(TrackSpawnLoop(i));
        }
    }

    IEnumerator TrackSpawnLoop(int trackIndex)
    {
        TrackConfig track = tracks[trackIndex];

        while (currentWaveIndices[trackIndex] < track.waves.Length)
        {
            Wave currentWave = track.waves[currentWaveIndices[trackIndex]];

            currentWaveIndices[trackIndex]++;
            UpdateWaveCounter();

            yield return StartCoroutine(SpawnWave(trackIndex, currentWave));
            yield return StartCoroutine(WaitForAllMobsDefeated(trackIndex));

            if (currentWaveIndices[trackIndex] < track.waves.Length)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        if (loopWaves)
        {
            currentWaveIndices[trackIndex] = 0;
            StartCoroutine(TrackSpawnLoop(trackIndex));
        }
    }

    IEnumerator SpawnWave(int trackIndex, Wave wave)
    {
        isSpawning[trackIndex] = true;
        activeMobsPerTrack[trackIndex].Clear();

        for (int i = 0; i < wave.mobCount; i++)
        {
            GameObject mob = SpawnMob(trackIndex, wave.mobPrefab);
            if (mob != null) activeMobsPerTrack[trackIndex].Add(mob);
            yield return new WaitForSeconds(1f / wave.spawnRate);
        }

        isSpawning[trackIndex] = false;
    }

    GameObject SpawnMob(int trackIndex, GameObject mobPrefab)
    {
        TrackConfig track = tracks[trackIndex];

        if (track.spawnPoints.Length == 0 || mobPrefab == null)
            return null;

        lastSpawnPointIndices[trackIndex] =
            (lastSpawnPointIndices[trackIndex] + 1) % track.spawnPoints.Length;
        Transform spawnPoint = track.spawnPoints[lastSpawnPointIndices[trackIndex]];

        GameObject mob = Instantiate(mobPrefab, spawnPoint.position, spawnPoint.rotation);
        MovementMobs movement = mob.GetComponent<MovementMobs>();

        if (movement != null)
        {
            if (lastSpawnPointIndices[trackIndex] == 0) 
            {
                movement.Initialize(track.waypoints);
            }
            else
            {
                movement.Initialize(track.altWaypoints);
            }
        }

        return mob;
    }

    IEnumerator WaitForAllMobsDefeated(int trackIndex)
    {
        while (true)
        {
            activeMobsPerTrack[trackIndex].RemoveAll(mob => mob == null);

            if (activeMobsPerTrack[trackIndex].Count == 0)
                yield break;

            yield return null;
        }
    }

    private void UpdateWaveCounter()
    {
        if (waveCounterText != null)
        {
            int totalWaves = tracks.Sum(t => t.waves.Length);
            int completedWaves = currentWaveIndices.Sum();
            waveCounterText.text = $"{completedWaves}/{totalWaves}";
        }
    }

    public bool AllWavesCompleted
    {
        get
        {
            if (currentWaveIndices == null || tracks == null) return false;

            for (int i = 0; i < tracks.Length; i++)
            {
                if (currentWaveIndices[i] < tracks[i].waves.Length)
                {
                    return false;
                }
            }
            return true;
        }
    }

    public bool IsSpawning
    {
        get
        {
            if (isSpawning == null) return false;
            return isSpawning.Any(x => x);
        }
    }
}