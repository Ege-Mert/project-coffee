using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// Coffee bean bag for adding beans to grinder
/// </summary>
public class CoffeeBeanBag : Clickable
{
    [SerializeField] private CoffeeGrinder targetGrinder;
    [SerializeField] private int beansPerClick = 1;
    // [SerializeField] private ParticleSystem beanParticles;
    // [SerializeField] private AudioSource beanPourSound;
    // [SerializeField] private Animator bagAnimator;
    
    // For visual bean animation
    [SerializeField] private GameObject beanPrefab;
    [SerializeField] private float beanJumpDuration = 0.5f;
    [SerializeField] private float beanJumpHeight = 100f;
    [SerializeField] private int beansToShow = 3;
    
    private void Start()
    {
        // Validate target grinder
        if (targetGrinder == null)
        {
            print("ERROR: Target Coffee Grinder is not set in Coffee Bean Bag!");
        }
    }
    
    protected override void OnClick()
    {
        print("Coffee Bean Bag clicked");
        
        // Validation check
        if (targetGrinder == null)
        {
            print("ERROR: Cannot add beans - target grinder is null!");
            return;
        }
        
        // // Visual feedback
        // if (beanParticles != null)
        // {
        //     beanParticles.Play();
        // }
        
        // // Animation
        // if (bagAnimator != null)
        // {
        //     bagAnimator.SetTrigger("Pour");
        // }
        
        // // Sound
        // if (beanPourSound != null)
        // {
        //     beanPourSound.Play();
        // }
        
        // Add beans directly first (to ensure it works even without animations)
        print($"Adding {beansPerClick} beans to grinder");
        targetGrinder.AddBeans(beansPerClick);
        
        // If we have a bean prefab, show the animation
        if (beanPrefab != null)
        {
            SpawnAnimatedBeans();
        }
    }
    
    private void SpawnAnimatedBeans()
    {
        // Get the positions for the animation
        Vector3 startPos = transform.position;
        Vector3 targetPos = targetGrinder.transform.position;
        
        // Create a sequence for slightly staggered animations
        Sequence beanSequence = DOTween.Sequence();
        
        for (int i = 0; i < beansToShow; i++)
        {
            // Create a bean
            GameObject bean = Instantiate(beanPrefab, transform.parent);
            RectTransform beanRect = bean.GetComponent<RectTransform>();
            
            // Set initial position
            beanRect.position = startPos;
            
            // Add slight random offset for natural look
            Vector3 randomOffset = new Vector3(
                Random.Range(-15f, 15f),
                Random.Range(-15f, 15f),
                0
            );
            
            // Calculate delay for staggered effect
            float delay = 0.05f * i;
            
            // Replace DOPath with DOJump
            beanSequence.Insert(delay, beanRect.DOJump(
                targetPos + randomOffset * 0.25f,  // endValue (target position with offset)
                beanJumpHeight,                    // jumpPower
                1,                                 // numJumps (single jump)
                beanJumpDuration)                  // duration
                .SetEase(Ease.OutQuad));
            
            // Scale effect
            beanSequence.Insert(delay, beanRect.DOScale(new Vector3(0.8f, 0.8f, 0.8f), beanJumpDuration * 0.3f)
                .SetLoops(2, LoopType.Yoyo));
            
            // Destroy the bean at the end
            beanSequence.InsertCallback(delay + beanJumpDuration, () => {
                Destroy(bean);
            });
        }
        
        beanSequence.Play();
    }
}