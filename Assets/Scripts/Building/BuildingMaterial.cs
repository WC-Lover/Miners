using UnityEngine;

public class BuildingMaterial : MonoBehaviour
{
    [SerializeField] private Material playerBuildingMaterial;
    [SerializeField] private Material enemyBuildingMaterial;
    [SerializeField] private Material neutralBuildingMaterial;
    [SerializeField] MeshRenderer[] meshRenderers;

    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        SetMaterial(enemyBuildingMaterial);
    }

    public void SetBuildingMaterial()
    {
        SetMaterial(playerBuildingMaterial);
    }

    public void SetNeutralBuildingMaterial()
    {
        SetMaterial(neutralBuildingMaterial);
    }

    private void SetMaterial(Material buildingMaterial)
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer renderer = meshRenderers[i];

            renderer.sharedMaterial = buildingMaterial;

            renderer.GetPropertyBlock(propertyBlock);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
