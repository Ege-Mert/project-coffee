using System.Collections;
using UnityEngine;
/// <summary>
/// Cup stack for creating new cups
/// </summary>
public class CupStack : Clickable
{
    [SerializeField] private GameObject cupPrefab;
    [SerializeField] private Transform cupSpawnPoint;
    [SerializeField] private int maxCups = 10;
    [SerializeField] private int maxActiveAtOnce = 3;
    [SerializeField] private ParticleSystem cupParticles;
    [SerializeField] private AudioSource cupSound;
    [SerializeField] private Animator stackAnimator;
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private Canvas gameCanvas; // Reference to the main canvas
    
    private int cupsCreated = 0;
    private int activeCount = 0;
    
    void Start()
    {
        // If no canvas is assigned, find the main canvas
        if (gameCanvas == null)
        {
            gameCanvas = FindObjectOfType<Canvas>();
            LogDebug($"Using main canvas: {(gameCanvas != null ? gameCanvas.name : "NONE FOUND")}");
        }
    }
    
    protected override void OnClick()
    {
        LogDebug($"Cup stack clicked. Active count: {activeCount}, Max: {maxActiveAtOnce}");
        
        if (activeCount >= maxActiveAtOnce)
        {
            UIManager.Instance.ShowNotification("Too many cups in use!");
            return;
        }
        
        // Create a cup
        if (cupPrefab != null)
        {
            // Spawn as a child of the main canvas to ensure visibility
            GameObject cupObj;
            
            if (gameCanvas != null)
            {
                // Spawn under the main canvas
                cupObj = Instantiate(cupPrefab, gameCanvas.transform);
                LogDebug($"Spawned cup under canvas: {gameCanvas.name}");
            }
            else
            {
                // Fallback to spawn under this object
                cupObj = Instantiate(cupPrefab);
                LogDebug($"Spawned cup without canvas (potentially invisible)");
            }
            
            // Position the cup at the spawn point if provided
            if (cupSpawnPoint != null)
            {
                // World position
                cupObj.transform.position = cupSpawnPoint.position;
                LogDebug($"Positioned cup at: {cupSpawnPoint.position}");
            }
            
            // Let the cup know which stack created it
            Cup cup = cupObj.GetComponent<Cup>();
            if (cup != null)
            {
                cup.SetSourceStack(this);
            }
            
            // Add a Draggable component if it doesn't have one
            if (cupObj.GetComponent<Draggable>() == null)
            {
                cupObj.AddComponent<Draggable>();
                LogDebug("Added Draggable component to cup");
            }
            
            // Make sure the cup is active and visible
            cupObj.SetActive(true);
            
            // Set initial scale
            cupObj.transform.localScale = Vector3.one;
            
            cupsCreated++;
            activeCount++;
            
            LogDebug($"Cup created. Active count now: {activeCount}");
            
            // Visual feedback
            if (cupParticles != null)
            {
                cupParticles.Play();
            }
            
            // Sound
            if (cupSound != null && cupSound.isActiveAndEnabled)
            {
                cupSound.Play();
            }
            
            // Animation
            if (stackAnimator != null)
            {
                stackAnimator.SetTrigger("Pop");
            }
            
            UIManager.Instance.ShowNotification("Cup created");
            
            // If at max cups, add a cooldown
            if (cupsCreated >= maxCups)
            {
                StartCoroutine(CupCooldownRoutine());
            }
        }
        else
        {
            LogDebug("ERROR: Cup prefab is not assigned!");
        }
    }
    
    // Called when a cup is destroyed
    public void OnCupDestroyed()
    {
        activeCount = Mathf.Max(0, activeCount - 1);
        LogDebug($"Cup destroyed. Active count now: {activeCount}");
    }
    
    private IEnumerator CupCooldownRoutine()
    {
        isActive = false;
        
        yield return new WaitForSeconds(5f);
        
        cupsCreated = 0;
        isActive = true;
    }
    
    private void LogDebug(string message)
    {
        if (debugLogs)
        {
            Debug.Log($"[CupStack] {message}");
        }
    }
}