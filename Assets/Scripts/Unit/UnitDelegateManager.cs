using System;

public class UnitDelegateManager
{
    // Owner
    public Action OnUnitSetUp;
    public Action<float> OnStaminaChanged;
    public Action<float> OnHealthChanged;
    public Action OnInteractionTargetDespawn;

    // Everybody
    public Action<bool> OnResourceGathered;
    public Action OnResourceUnload;
    public Action OnDespawn;

    public void DisableAllDelegates()
    {
        // Owner
        OnUnitSetUp = null;
        OnStaminaChanged = null;
        OnHealthChanged = null;
        OnInteractionTargetDespawn = null;
        // Everybody
        OnResourceGathered = null;
        OnResourceUnload = null;
        OnDespawn = null;
    }
}
