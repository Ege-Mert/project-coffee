/// <summary>
/// Base interface for all interactive UI elements
/// </summary>
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening; // Make sure to import DOTween from Asset Store

public interface IInteractiveElement
{
    bool CanInteract();
    void OnInteractionStart();
    void OnInteractionEnd();
}