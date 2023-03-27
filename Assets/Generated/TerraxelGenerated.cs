using Unity.Collections;
using Unity.Mathematics;
using Terraxel.DataStructures;
using System.Runtime.CompilerServices;


public class TerraxelGenerated
{
    static readonly FastNoiseLite props0 = new FastNoiseLite(1337, 0.0070f, 2, 2.0000f, 0.3000f);
static readonly FastNoiseLite props1 = new FastNoiseLite(1337, 0.0070f, 2, 2.0000f, 0.3000f);
static readonly FastNoiseLite props2 = new FastNoiseLite(1337, 0.0030f, 3, 3.0000f, 0.3000f);
static readonly FastNoiseLite props4 = new FastNoiseLite(1337, 0.0004f, 2, 3.0000f, 0.5000f);
static readonly FastNoiseLite props5 = new FastNoiseLite(1337, 0.0005f, 4, 3.0000f, 0.5000f);
static readonly FastNoiseLite props7 = new FastNoiseLite(1337, 0.0004f, 3, 5.0000f, 0.3000f);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte GenerateDensity(float3 pos){
        float2 pos2D = new float2(pos.x, pos.z);
        return DensityGenerator.HeightMapToIsosurface(pos, TerraxelGenerated.GenerateDensity(pos2D));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GenerateDensity(float2 pos2D){
        float op0 = DensityGenerator.SurfaceNoise2D(pos2D + new float2(253.0000f,43.0000f), 19.2000f, props0);
float op1 = DensityGenerator.SurfaceNoise2D(pos2D, 19.2000f, props1);
float op2 = DensityGenerator.SurfaceNoise2D(pos2D + new float2(op1,op0), 48.0000f, props2);
float op3 = (op2 * 2);
float op4 = DensityGenerator.SurfaceNoise2D(pos2D + new float2(op3,op3), 24.0000f, props4);
float op5 = DensityGenerator.SurfaceNoise2D(pos2D + new float2(op4,op4), 192.0000f, props5);
float op6 = (op5 * 2);
float op7 = DensityGenerator.SurfaceNoise2D(pos2D + new float2(op6,op6), 120.0000f, props7);


        return op7;
    }
}