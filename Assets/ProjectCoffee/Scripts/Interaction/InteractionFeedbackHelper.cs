using UnityEngine;
using DG.Tweening;

/// <summary>
/// Helper class for handling common interaction feedback effects
/// </summary>
public static class InteractionFeedbackHelper
{
    /// <summary>
    /// Apply a "click" scale animation to a transform
    /// </summary>
    public static void PlayClickAnimation(Transform transform, float duration = 0.1f, float scaleFactor = 0.9f)
    {
        if (transform == null) return;
        
        // Kill any existing animations on this transform
        DOTween.Kill(transform);
        
        // Sequence for press-and-release effect
        transform.DOScale(Vector3.one * scaleFactor, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
            });
    }
    
    /// <summary>
    /// Apply a "grow" scale animation when starting interaction
    /// </summary>
    public static void PlayStartInteractionAnimation(Transform transform, float duration = 0.2f, float scaleFactor = 1.1f)
    {
        if (transform == null) return;
        
        // Kill any existing animations on this transform
        DOTween.Kill(transform);
        
        transform.DOScale(Vector3.one * scaleFactor, duration).SetEase(Ease.OutBack);
    }
    
    /// <summary>
    /// Apply a "shrink" scale animation when ending interaction
    /// </summary>
    public static void PlayEndInteractionAnimation(Transform transform, float duration = 0.2f)
    {
        if (transform == null) return;
        
        // Kill any existing animations on this transform
        DOTween.Kill(transform);
        
        transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
    }
    
    /// <summary>
    /// Play a bounce animation for feedback (useful for invalid drops, etc.)
    /// </summary>
    public static void PlayBounceAnimation(Transform transform, float duration = 0.3f, float strength = 0.2f)
    {
        if (transform == null) return;
        
        // Kill any existing animations on this transform
        DOTween.Kill(transform);
        
        transform.DOPunchScale(Vector3.one * strength, duration, 2, 0.5f);
    }
    
    /// <summary>
    /// Play a return-to-position animation for draggable items
    /// </summary>
    public static void PlayReturnAnimation(RectTransform rectTransform, Vector2 targetPosition, float duration = 0.3f)
    {
        if (rectTransform == null) return;
        
        // Kill any existing animations on this transform
        DOTween.Kill(rectTransform);
        
        rectTransform.DOAnchorPos(targetPosition, duration).SetEase(Ease.OutBack);
    }
    
    /// <summary>
    /// Play audio if available
    /// </summary>
    public static void PlaySound(AudioSource audioSource)
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
    
    /// <summary>
    /// Stop audio if playing
    /// </summary>
    public static void StopSound(AudioSource audioSource)
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}