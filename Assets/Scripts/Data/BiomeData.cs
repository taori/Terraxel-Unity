using UnityEngine;

namespace Assets.Scripts.Data
{
	[CreateAssetMenu(fileName = "Biome Data", menuName = "Terraxel/Biome", order = 1), System.Serializable]
	public class BiomeData : ScriptableObject
	{
		public string biomeName;
		public InstancingData[] instances;

		public string GetGeneratorString(){
			string result = "";
			for(int i = 0; i < 5; i++){
				if(i > instances.Length-1){
					result += "public static readonly NativeInstanceData data" + i.ToString() + " = new NativeInstanceData();";
					continue;
				} 
				if(instances[i] != null){
					result += "public static readonly NativeInstanceData data" + i.ToString() + " = new NativeInstanceData("+TerrainGeneration.Utils.float2ToString(instances[i].angleLimit)+", "+
					          TerrainGeneration.Utils.float3x2ToString(instances[i].sizeVariation)+", "+TerrainGeneration.Utils.floatToString(instances[i].density)+", "+instances[i].maxLod+", "+instances[i].uniformDensity.ToString().ToLower()+");" + System.Environment.NewLine + "\t";
				}
			}
			return result;
		}
	}
}
