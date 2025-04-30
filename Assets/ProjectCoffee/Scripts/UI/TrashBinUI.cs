using UnityEngine;

/// <summary>
/// Simple trash bin implementation for testing
/// </summary>
public class TrashBinUI : MonoBehaviour
{
    [SerializeField] private DropZoneUI trashZone;
    [SerializeField] private AudioSource trashSound;
    [SerializeField] private ParticleSystem trashParticles;
    [SerializeField] private Animator trashAnimator;
    
    private void Start()
    {
        if (trashZone != null)
        {
            // Configure to accept any draggable item
            //trashZone.CanAccept = (item) => true;
            
            // Custom implementation through a delegate would be better
            // For now, we'll use the inspector to connect OnItemDropped to our function
        }
    }
    
    // Connect this method to the trashZone's OnDrop event in the inspector
    public void OnItemTrashed(DraggableUI item)
    {
        // Visual and audio feedback
        if (trashSound != null)
        {
            trashSound.Play();
        }
        
        if (trashParticles != null)
        {
            trashParticles.Play();
        }
        
        if (trashAnimator != null)
        {
            trashAnimator.SetTrigger("Trash");
        }
        
        // Destroy the item
        Destroy(item.gameObject);
        
        UIManager.Instance.ShowNotification("Item trashed");
    }
}