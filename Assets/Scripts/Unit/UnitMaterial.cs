using UnityEngine;

namespace Assets.Scripts.Unit
{

    public class UnitMaterial : MonoBehaviour
    {
        [SerializeField] private Material playerMaterial;
        [SerializeField] private Material enemyMaterial;
        [SerializeField] MeshRenderer[] meshRenderers;

        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        public void SetUnitMaterial(bool isOwner)
        {
            Material material = isOwner ? playerMaterial : enemyMaterial;

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                MeshRenderer renderer = meshRenderers[i];
                renderer.sharedMaterial = material;

                renderer.GetPropertyBlock(propertyBlock);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}