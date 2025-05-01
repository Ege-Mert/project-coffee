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
    
    private int cupsCreated = 0;
    private int activeCount = 0;
    
    protected override void OnClick()
    {
        if (activeCount >= maxActiveAtOnce)
        {
            UIManager.Instance.ShowNotification("Too many cups in use!");
            return;
        }
        
        // Create a cup
        if (cupPrefab != null && cupSpawnPoint != null)
        {
            GameObject cupObj = Instantiate(cupPrefab, cupSpawnPoint.position, Quaternion.identity, cupSpawnPoint);
            cupsCreated++;
            activeCount++;
            
            // Visual feedback
            if (cupParticles != null)
            {
                cupParticles.Play();
            }
            
            // Sound
            if (cupSound != null)
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
    }
    
    // Called when a cup is destroyed
    public void OnCupDestroyed()
    {
        activeCount = Mathf.Max(0, activeCount - 1);
    }
    
    private IEnumerator CupCooldownRoutine()
    {
        isActive = false;
        
        yield return new WaitForSeconds(5f);
        
        cupsCreated = 0;
        isActive = true;
    }
}