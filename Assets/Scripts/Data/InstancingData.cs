using Assets.Scripts.TerrainGeneration;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.Data
{
	[CreateAssetMenu(fileName = "Instancing Data", menuName = "Terraxel/Rendering Instance", order = 1), System.Serializable]
	public class InstancingData : ScriptableObject{
		[SerializeField]
		public MeshMaterialPair[] renderData;
		[SerializeField]
		public float2 angleLimit;
    
		[SerializeField]
		public float3x2 sizeVariation;
		[SerializeField]
		public bool uniformDensity;
		[SerializeField, Range(0f, 100f)]
		public float density;
		[SerializeField]
		public int maxLod;
		[SerializeField]
		public GameObject colliderObject;

		public void OnValidate(){
		}
	}
}
