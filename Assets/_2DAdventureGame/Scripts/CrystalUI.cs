using System.Collections;
using UnityEngine;
using TMPro; // Бібліотека для роботи з TextMeshPro
using System.Collections.Generic;

public class CrystalUI : MonoBehaviour
{
    public static CrystalUI instance;
    public TextMeshProUGUI crystalText;
    public Transform iconTransform;
    public GameObject crystalPrefab;
    public int randomCrystalCount = 29;
    public int crystalsNearPlayerAtStart = 10;
    public float nearPlayerSpawnRadius = 4.05f;
    public float nearPlayerVerticalRadius = 2.55f;
    public float crystalRespawnDelay = 5f;
    public Vector2 randomCrystalAreaMin = new Vector2(-22f, 4f);
    public Vector2 randomCrystalAreaMax = new Vector2(24f, 26f);
    public float minimumCrystalSpacing = 1.8f;
    public float minimumDistanceFromPlayer = 2.5f;

    private int crystalCount = 0;
    private Coroutine iconPunchRoutine;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        UpdateUI();
        ScatterCrystalsRandomly();
    }

    private void ScatterCrystalsRandomly()
    {
        if (crystalPrefab == null)
        {
            Debug.LogError("CrystalUI has no crystal prefab assigned in MainScene.");
            return;
        }

        if (randomCrystalCount <= 0)
            return;

        BreakableCrystal[] existingCrystals = FindObjectsByType<BreakableCrystal>(
            FindObjectsSortMode.None);
        List<Vector2> occupiedPositions = new List<Vector2>();

        PlayerController player = FindFirstObjectByType<PlayerController>();
        Vector2 playerPosition = player != null
            ? (Vector2)player.transform.position
            : Vector2.zero;

        float minX = Mathf.Min(randomCrystalAreaMin.x, randomCrystalAreaMax.x);
        float maxX = Mathf.Max(randomCrystalAreaMin.x, randomCrystalAreaMax.x);
        float minY = Mathf.Min(randomCrystalAreaMin.y, randomCrystalAreaMax.y);
        float maxY = Mathf.Max(randomCrystalAreaMin.y, randomCrystalAreaMax.y);

        // Move the crystals already placed in MainScene so their locations vary each run.
        foreach (BreakableCrystal crystal in existingCrystals)
        {
            if (crystal == null)
                continue;

            Vector2 previousPosition = crystal.transform.position;
            if (TryGetRandomCrystalPosition(
                    false, playerPosition, player != null, occupiedPositions,
                    minX, maxX, minY, maxY, out Vector2 randomPosition))
            {
                crystal.transform.position = randomPosition;
                occupiedPositions.Add(randomPosition);
            }
            else
            {
                occupiedPositions.Add(previousPosition);
            }
        }

        int nearPlayerTarget = player != null
            ? Mathf.Clamp(crystalsNearPlayerAtStart, 0, randomCrystalCount)
            : 0;
        int spawnedNearPlayer = SpawnCrystalGroup(
            nearPlayerTarget, true, player != null, playerPosition,
            occupiedPositions, minX, maxX, minY, maxY);
        int remainingCount = randomCrystalCount - spawnedNearPlayer;
        int spawnedAcrossMap = SpawnCrystalGroup(
            remainingCount, false, player != null, playerPosition,
            occupiedPositions, minX, maxX, minY, maxY);

        int totalCrystals = existingCrystals.Length + spawnedNearPlayer + spawnedAcrossMap;
        Debug.Log($"MainScene crystal spawn: {totalCrystals} total ({existingCrystals.Length} placed + {spawnedNearPlayer + spawnedAcrossMap} spawned at start).");
    }

    private int SpawnCrystalGroup(
        int targetCount,
        bool nearPlayer,
        bool hasPlayer,
        Vector2 playerPosition,
        List<Vector2> occupiedPositions,
        float minX,
        float maxX,
        float minY,
        float maxY)
    {
        if (targetCount <= 0)
            return 0;

        if (nearPlayer)
        {
            return SpawnBalancedNearPlayerGroup(
                targetCount, playerPosition, hasPlayer, occupiedPositions);
        }

        return SpawnRandomCrystalGroup(
            targetCount, false, playerPosition, hasPlayer,
            occupiedPositions, minX, maxX, minY, maxY);
    }

    private int SpawnBalancedNearPlayerGroup(
        int targetCount,
        Vector2 playerPosition,
        bool hasPlayer,
        List<Vector2> occupiedPositions)
    {
        // Try evenly spaced ellipse layouts first; the phase varies between runs.
        float startingPhase = Random.Range(76f, 84f);
        for (int phaseOffset = 0; phaseOffset < 360; phaseOffset += 2)
        {
            float phase = startingPhase + phaseOffset;
            List<Vector2> layout = new List<Vector2>(targetCount);
            bool layoutIsClear = true;

            for (int i = 0; i < targetCount; i++)
            {
                float angle = (phase + i * 360f / targetCount) * Mathf.Deg2Rad;
                Vector2 position = playerPosition + new Vector2(
                    Mathf.Cos(angle) * nearPlayerSpawnRadius,
                    Mathf.Sin(angle) * nearPlayerVerticalRadius);

                if (!IsCrystalPositionAvailable(
                        position, playerPosition, hasPlayer, occupiedPositions,
                        minimumCrystalSpacing))
                {
                    layoutIsClear = false;
                    break;
                }

                for (int j = 0; j < layout.Count; j++)
                {
                    if (Vector2.Distance(position, layout[j]) < minimumCrystalSpacing)
                    {
                        layoutIsClear = false;
                        break;
                    }
                }

                if (!layoutIsClear)
                    break;

                layout.Add(position);
            }

            if (!layoutIsClear)
                continue;

            foreach (Vector2 position in layout)
            {
                Instantiate(crystalPrefab, position, Quaternion.identity);
                occupiedPositions.Add(position);
            }

            return layout.Count;
        }

        // If a placed scene crystal blocks every ring phase, fill nearby open spots.
        return SpawnRandomCrystalGroup(
            targetCount, true, playerPosition, hasPlayer, occupiedPositions,
            playerPosition.x - nearPlayerSpawnRadius,
            playerPosition.x + nearPlayerSpawnRadius,
            playerPosition.y - nearPlayerVerticalRadius,
            playerPosition.y + nearPlayerVerticalRadius);
    }

    private int SpawnRandomCrystalGroup(
        int targetCount,
        bool nearPlayer,
        Vector2 playerPosition,
        bool hasPlayer,
        List<Vector2> occupiedPositions,
        float minX,
        float maxX,
        float minY,
        float maxY)
    {
        int attempts = 0;
        int placed = 0;
        int maxAttempts = targetCount * 100;

        while (placed < targetCount && attempts < maxAttempts)
        {
            attempts++;
            if (!TryGetRandomCrystalPosition(
                    nearPlayer, playerPosition, hasPlayer, occupiedPositions,
                    minX, maxX, minY, maxY, out Vector2 position))
                continue;

            Instantiate(crystalPrefab, position, Quaternion.identity);
            occupiedPositions.Add(position);
            placed++;
        }

        return placed;
    }

    private bool IsCrystalPositionAvailable(
        Vector2 position,
        Vector2 playerPosition,
        bool hasPlayer,
        List<Vector2> occupiedPositions,
        float minimumSpacing)
    {
        if (hasPlayer && Vector2.Distance(position, playerPosition) < minimumDistanceFromPlayer)
            return false;

        for (int i = 0; i < occupiedPositions.Count; i++)
        {
            if (Vector2.Distance(position, occupiedPositions[i]) < minimumSpacing)
                return false;
        }

        return true;
    }

    private bool TryGetRandomCrystalPosition(
        bool nearPlayer,
        Vector2 playerPosition,
        bool hasPlayer,
        List<Vector2> occupiedPositions,
        float minX,
        float maxX,
        float minY,
        float maxY,
        out Vector2 position)
    {
        const int maxPositionAttempts = 100;
        for (int attempt = 0; attempt < maxPositionAttempts; attempt++)
        {
            if (nearPlayer)
            {
                Vector2 offset = Random.insideUnitCircle;
                position = playerPosition + new Vector2(
                    offset.x * nearPlayerSpawnRadius,
                    offset.y * nearPlayerVerticalRadius);
            }
            else
            {
                position = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            }

            if (hasPlayer && Vector2.Distance(position, playerPosition) < minimumDistanceFromPlayer)
                continue;

            bool tooCloseToAnotherCrystal = false;
            for (int i = 0; i < occupiedPositions.Count; i++)
            {
                if (Vector2.Distance(position, occupiedPositions[i]) < minimumCrystalSpacing)
                {
                    tooCloseToAnotherCrystal = true;
                    break;
                }
            }

            if (!tooCloseToAnotherCrystal)
                return true;
        }

        position = Vector2.zero;
        return false;
    }

    public void ScheduleCrystalRespawn()
    {
        StartCoroutine(RespawnCrystalAfterDelay());
    }

    private System.Collections.IEnumerator RespawnCrystalAfterDelay()
    {
        yield return new WaitForSeconds(crystalRespawnDelay);
        SpawnReplacementCrystal();
    }

    private void SpawnReplacementCrystal()
    {
        if (crystalPrefab == null)
            return;

        BreakableCrystal[] existingCrystals = FindObjectsByType<BreakableCrystal>(
            FindObjectsSortMode.None);
        List<Vector2> occupiedPositions = new List<Vector2>();
        foreach (BreakableCrystal crystal in existingCrystals)
        {
            if (crystal != null)
                occupiedPositions.Add(crystal.transform.position);
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();
        Vector2 playerPosition = player != null
            ? (Vector2)player.transform.position
            : Vector2.zero;
        float minX = Mathf.Min(randomCrystalAreaMin.x, randomCrystalAreaMax.x);
        float maxX = Mathf.Max(randomCrystalAreaMin.x, randomCrystalAreaMax.x);
        float minY = Mathf.Min(randomCrystalAreaMin.y, randomCrystalAreaMax.y);
        float maxY = Mathf.Max(randomCrystalAreaMin.y, randomCrystalAreaMax.y);

        if (TryGetRandomCrystalPosition(
                false, playerPosition, player != null, occupiedPositions,
                minX, maxX, minY, maxY, out Vector2 position))
        {
            Instantiate(crystalPrefab, position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Could not find a free position for a respawned crystal.");
        }
    }

    public void AddCrystal(int amount = 1)
    {
        crystalCount += amount;
        UpdateUI();

        if (iconTransform != null)
        {
            if (iconPunchRoutine != null)
                StopCoroutine(iconPunchRoutine);

            iconPunchRoutine = StartCoroutine(PunchIcon());
        }
    }

    void UpdateUI()
    {
        if (crystalText != null)
        {
            crystalText.text = "x " + crystalCount.ToString();
        }
    }

    private IEnumerator PunchIcon()
    {
        iconTransform.localScale = Vector3.one * 1.3f;
        float elapsed = 0f;
        const float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            iconTransform.localScale = Vector3.Lerp(
                Vector3.one * 1.3f, Vector3.one, elapsed / duration);
            yield return null;
        }

        iconTransform.localScale = Vector3.one;
        iconPunchRoutine = null;
    }
}
