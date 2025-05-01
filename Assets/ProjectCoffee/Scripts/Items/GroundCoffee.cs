using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// Class for ground coffee with multiple size stages
/// </summary>
public class GroundCoffee : Draggable
{
    [Header("Coffee Settings")]
    [SerializeField] private List<Sprite> grindStageSprites;
    [SerializeField] private Image coffeeImage;
    [SerializeField] private float[] stageAmounts = { 6f, 12f, 18f }; // Default values, can be overridden
    
    public enum GrindSize { Small, Medium, Large }
    
    private GrindSize currentSize = GrindSize.Small;
    private float coffeeAmount = 6f;
    
    protected override void Awake()
    {
        base.Awake();
        UpdateVisual();
        
        // Debug check for sprites
        if (grindStageSprites == null || grindStageSprites.Count == 0)
        {
            print("ERROR: No grind stage sprites assigned to GroundCoffeeUI!");
        }
        else
        {
            print($"GroundCoffeeUI has {grindStageSprites.Count} sprites assigned");
            
            // Check for null sprites
            for (int i = 0; i < grindStageSprites.Count; i++)
            {
                if (grindStageSprites[i] == null)
                {
                    print($"ERROR: Sprite at index {i} is null!");
                }
            }
        }
        
        if (coffeeImage == null)
        {
            print("ERROR: Coffee Image is not assigned in GroundCoffeeUI!");
        }
    }
    
    public void SetGrindSize(GrindSize size)
    {
        print($"Setting grind size to {size}");
        currentSize = size;
        
        // If not explicitly set by CoffeeGrinder, use default values
        if ((int)size < stageAmounts.Length)
        {
            coffeeAmount = stageAmounts[(int)size];
        }
        
        UpdateVisual();
    }
    
    /// <summary>
    /// Set an explicit amount for this ground coffee, overriding the default for the size
    /// </summary>
    public void SetAmount(float amount)
    {
        coffeeAmount = amount;
        print($"Set explicit coffee amount to {coffeeAmount}g");
    }
    
    public void UpgradeSize()
    {
        GrindSize oldSize = currentSize;
        
        if (currentSize < GrindSize.Large)
        {
            currentSize = (GrindSize)((int)currentSize + 1);
            
            // If not explicitly set, use default values
            if ((int)currentSize < stageAmounts.Length)
            {
                coffeeAmount = stageAmounts[(int)currentSize];
            }
            
            UpdateVisual();
            
            // Animate growth
            transform.DOScale(Vector3.one * 1.2f, 0.1f).SetLoops(2, LoopType.Yoyo);
            
            print($"Upgraded coffee size from {oldSize} to {currentSize}");
        }
        else
        {
            print($"Coffee already at maximum size: {currentSize}");
        }
    }
    
    public GrindSize GetGrindSize()
    {
        return currentSize;
    }
    
    public float GetAmount()
    {
        return coffeeAmount;
    }
    
    private void UpdateVisual()
    {
        // Update sprite based on current size
        if (coffeeImage != null && grindStageSprites != null && grindStageSprites.Count > (int)currentSize)
        {
            print($"Updating coffee visual to size {currentSize} with sprite index {(int)currentSize}");
            
            Sprite oldSprite = coffeeImage.sprite;
            coffeeImage.sprite = grindStageSprites[(int)currentSize];
            
            if (oldSprite == coffeeImage.sprite && oldSprite != null)
            {
                print("WARNING: Sprite didn't change after update!");
            }
            
            // Scale based on size
            float scale = 0.8f + ((int)currentSize * 0.2f);
            coffeeImage.transform.localScale = new Vector3(scale, scale, 1f);
            
            print($"Set coffee image scale to {scale}");
        }
        else
        {
            print("ERROR: Cannot update visual - missing references or sprite index out of range");
            
            if (coffeeImage == null)
                print("- coffeeImage is null");
                
            if (grindStageSprites == null)
                print("- grindStageSprites is null");
                
            if (grindStageSprites != null && grindStageSprites.Count <= (int)currentSize)
                print($"- Not enough sprites: have {grindStageSprites.Count}, need {(int)currentSize + 1}");
        }
    }
}