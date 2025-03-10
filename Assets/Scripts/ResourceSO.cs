using UnityEngine;

[CreateAssetMenu(menuName = "Resources/Resource Type")]
public class ResourceSO : ScriptableObject
{
    public Resource.ResourceType type;
    public Material material;
    public int resourceWeight;
    public float interactionDistance;
    public AudioClip pickupSound;
}