using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Terraxel;
using UnityEngine.Rendering;
using System;
using Terraxel.DataStructures;
using Terraxel.WorldGeneration;
using System.Collections.Generic;
public class Chunk2D : BaseChunk
{
    
    private Mesh chunkMesh;
    public SimpleMeshData meshData;
    static Material chunkMaterial;
    public const int vertexCount = 4225;
    public const int indexCount = 6534 * 4;
    Matrix4x4 localMatrix;
    NativeReference<bool> isEmpty;
    VertexAttributeDescriptor[] layout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 1)
            };
    public override bool CanBeCreated{
        get{
            return IsReady;
        }
    }
    public Chunk2D(BoundingBox bounds, int depth)
    : base(bounds, depth){
        chunkMesh = new Mesh();
    }
    static Chunk2D(){
            chunkMaterial = Resources.Load("Materials/TerrainSimple", typeof(Material)) as Material;
    }

    protected override void OnScheduleMeshUpdate()
    {
        /*if(WorldPosition.y != 0){
            OnMeshReady();
            FreeChunkMesh();
            return;
        }*/
        isEmpty = new NativeReference<bool>(true, Allocator.TempJob);
        meshData = MemoryManager.GetSimpleMeshData();
        var pos = (int3)WorldPosition;
        var noiseJob = new NoiseJob2D{
            offset = new float2(pos.x, pos.z) - depthMultiplier / 2,
            depthMultiplier = depthMultiplier / 2,
            size = (ChunkManager.chunkResolution * 2) + 3,
            heightMap = meshData.heightMap,
            seed = TerraxelWorld.seed
        };
        base.ScheduleParallelForJob(noiseJob, 4489);
        var meshJob = new Mesh2DJob(){
            heightMap = meshData.heightMap,
            chunkSize = (ChunkManager.chunkResolution * 2) + 1,
            vertices = meshData.vertexBuffer,
            indices = meshData.indexBuffer,
            chunkPos = (int3)WorldPosition,
            depthMultiplier = depthMultiplier / 2,
            isEmpty = isEmpty,
            instancingData = instanceDatas,
            rng = base.rng,
            renderBounds = renderBoundsData,
            generateInstances = depth == treeDepth
        };
        base.ScheduleJobFor(meshJob, vertexCount, true);
    }
    public override void ApplyMesh()
    {
        if(isEmpty.Value){
            isEmpty.Dispose();
            OnMeshReady();
            FreeChunkMesh();
            return;
        }
        isEmpty.Dispose();
        if(onMeshReady == OnMeshReadyAction.ALERT_PARENT)
            SetActive(false);
        localMatrix = Matrix4x4.TRS(WorldPosition, Quaternion.identity, new float3(1, 1, 1));
        //propertyBlock.SetInt("_DepthMultiplier", depthMultiplier);
        chunkMesh.SetVertexBufferParams(vertexCount, layout);
        chunkMesh.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
        chunkMesh.SetVertexBufferData(meshData.vertexBuffer, 0, 0, vertexCount,0, MESH_UPDATE_FLAGS);
        chunkMesh.SetIndexBufferData(meshData.indexBuffer, 0, 0, indexCount);
        desc.indexCount = indexCount;
        chunkMesh.SetSubMesh(0, desc, MESH_UPDATE_FLAGS);
        var bounds = new Bounds(new float3(ChunkManager.chunkResolution * depthMultiplier / 2), region.bounds);
        chunkMesh.bounds = bounds;
        OnMeshReady();
    }
    public override void RenderChunk()
    {
        if(depth == treeDepth) base.RenderInstances();
        if(!active || chunkState != ChunkState.READY) return;
        Graphics.DrawMesh(chunkMesh, localMatrix, chunkMaterial, 0, null, 0, propertyBlock, true, true, true);
    }
    protected override void OnFreeBuffers()
    {
        chunkMesh.Clear();
        hasMesh = false;
        if(meshData != null && meshData.IsCreated)
            MemoryManager.ReturnSimpleMeshData(meshData);
        meshData = null;
    }
}
