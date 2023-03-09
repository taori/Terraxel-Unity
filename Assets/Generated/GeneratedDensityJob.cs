using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using WorldGeneration.DataStructures;


[BurstCompile]
public struct GeneratedDensityJob : IJobParallelFor
{
    [ReadOnly] public float3 offset;
    [ReadOnly] public int depthMultiplier;
    [ReadOnly] public int size;
    [NativeDisableParallelForRestriction, WriteOnly]
    public DensityResultData data;
    [ReadOnly] public NoiseProperties noiseProperties;
    [ReadOnly] public bool allowEmptyOrFull;

    public void Execute(int index)
    {
        var pos = Utils.IndexToXyz(index, size) * depthMultiplier + offset;
        var value = GenerateDensity(pos);
        //var value = DensityGenerator.FinalNoise(Utils.IndexToXyz(index, size) * depthMultiplier + offset, noiseProperties);
        data.densityMap[index] = value;
        if(!allowEmptyOrFull) return;
        if(value != 127){
            data.isEmpty.Value = false;
        }
        if(value != -127){
            data.isFull.Value = false;
        }
    }
    public static sbyte GenerateDensity(float3 pos){
        return DensityGenerator.FinalNoise(pos + new float3((DensityGenerator.FinalNoise(pos + new float3(0.0000f,0,0.0000f), 1.0000f, 0.0006f, -999875, 2,11) * 3),0,0.0000f), 24.0000f, 0.0060f, -999875, 2,11);
    }
}