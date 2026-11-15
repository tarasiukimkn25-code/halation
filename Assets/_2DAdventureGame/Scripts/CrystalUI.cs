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
    public int randomCrystalCount = 8;
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
        int targetCount = Mathf.Max(0, randomCrystalCount);
        BreakableCrystal[] existingCrystals = FindObjectsByType<BreakableCrystal>(FindObjectsSortMode.None);
        List<BreakableCrystal> candidates = new List<BreakableCrystal>(existingCrystals);

        // MainScene contains hand-placed crystals. Shuffle them so the same eight
        // do not survive every run, then remove extras before placing the start set.
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (candidates[i], candidates[swapIndex]) = (candidates[swapIndex], candidates[i]);
        }

        int selectedCount = Mathf.Min(targetCount, candidates.Count);
        for (int i = selectedCount; i < candidates.Count; i++)
        {
            if (candidates[i] != null)
                Destroy(candidates[i].gameObject);
        }

        List<Vector2> occupiedPositions = new List<Vector2>();
        PlayerController player = FindFirstObjectByType<PlayerController>();
        Vector2 playerPosition = player != null
            ? (Vector2)player.transform.position
            : Vector2.zero;

        float minX = Mathf.Min(randomCrystalAreaMin.x, randomCrystalAreaMax.x);
        float maxX = Mathf.Max(randomCrystalAreaMin.x, randomCrystalAreaMax.x);
        float minY = Mathf.Min(randomCrystalAreaMin.y, randomCrystalAreaMax.y);
        float maxY = Mathf.Max(randomCrystalAreaMin.y, randomCrystalAreaMax.y);

        // Randomize the selected scene crystals across the map.
        for (int i = 0; i < selectedCount; i++)
        {
            BreakableCrystal crystal = candidates[i];
            if (crystal == null)
                continue;

            if (TryGetRandomCrystalPosition(
                    playerPosition, player != null, occupiedPositions,
                    minX, maxX, minY, maxY, out Vector2 randomPosition))
            {
                crystal.transform.position = randomPosition;
                occupiedPositions.Add(randomPosition);
            }
            else
            {
                Destroy(crystal.gameObject);
            }
        }

        int attempts = 0;
        int maxAttempts = Mathf.Max(targetCount * 200, 200);
        while (occupiedPositions.Count < targetCount && crystalPrefab != null && attempts < maxAttempts)
        {
            attempts++;
            if (!TryGetRandomCrystalPosition(
                    playerPosition, player != null, occupiedPositions,
                    minX, maxX, minY, maxY, out Vector2 randomPosition))
                continue;

            Instantiate(crystalPrefab, randomPosition, Quaternion.identity);
            occupiedPositions.Add(randomPosition);
        }

        if (occupiedPositions.Count != targetCount)
        {
            Debug.LogWarning($"MainScene placed {occupiedPositions.Count} of {targetCount} starting crystals. Check the spawn area and crystal prefab.");
        }
    }

    private bool TryGetRandomCrystalPosition(
        Vector2 playerPosition,
        bool hasPlayer,
        List<Vector2> occupiedPositions,
        float minX,
        float maxX,
        float minY,
        float maxY,
        out Vector2 position)
    {
        const int maxPositionAttempts = 200;
        for (int attempt = 0; attempt < maxPositionAttempts; attempt++)
        {
            position = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));

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
                playerPosition, player != null, occupiedPositions,
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
