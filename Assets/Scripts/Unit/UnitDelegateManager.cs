using System;

public class UnitDelegateManager
{
    // Owner
    public Action OnUnitSetUp;
    public Action<float> OnStaminaChanged;
    public Action<float> OnHealthChanged;

    // Everybody
    // Unsubscribe in UI if HolyResource gathered
    public Action<bool> OnResourceGathered;
    // Unsubscribe in UI
    public Action OnResourceUnload;


    public void DisableAllDelegates()
    {
        // Owner
        OnUnitSetUp = null;
        OnStaminaChanged = null;
        OnHealthChanged = null;
        // Everybody
        OnResourceGathered = null;
        OnResourceUnload = null;
    }
}
