using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Terraxel;
using UnityEngine.Rendering;
using System;
using Terraxel.DataStructures;
using System.Collections.Generic;

public abstract class BaseChunk : Octree
{
    public const MeshUpdateFlags MESH_UPDATE_FLAGS = MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds;
    protected bool active = true;
    protected Unity.Mathematics.Random rng;
    //public Matrix4x4[] _grassPositions;
    protected NativeReference<float3x2> renderBoundsData;
    public Bounds renderBounds;
    float3x2 boundSource;
    public ChunkState chunkState = ChunkState.INVALID;
    public OnMeshReadyAction onMeshReady = OnMeshReadyAction.ALERT_PARENT;
    public DisposeState disposeStatus = DisposeState.NOTHING;
    public float genTime;
    public int vertCount;
    public int idxCount;
    public bool hasMesh;
    public byte dirMask;
    public JobInstancingData instanceDatas;
    public InstancedRenderer[] instanceRenderers;
    public abstract bool CanBeCreated{get;}
    protected MaterialPropertyBlock propertyBlock;
    protected int treeDepth = 4;
    
    //public NativeList<Matrix4x4> grassPositions;
    protected SubMeshDescriptor desc = new SubMeshDescriptor();
    public BaseChunk(BoundingBox bounds, int depth)
    : base(bounds, depth){
        desc.topology = MeshTopology.Triangles;
        chunkState = ChunkState.INVALID;
        disposeStatus = DisposeState.NOTHING;
        
        var biomeData = TerraxelWorld.GetBiomeData(0);
        instanceRenderers = new InstancedRenderer[5];
        propertyBlock = new MaterialPropertyBlock();
        rng = new Unity.Mathematics.Random((uint)TerraxelWorld.seed);
        if(this is Chunk2D && depth != treeDepth) return;
        for(int i = 0; i < 5; i++){
            if(BiomesGenerated.Get(i).density > 0){
                instanceRenderers[i] = new InstancedRenderer(biomeData.instances[i].renderData, ShadowCastingMode.On);
            }
        }
    }
    public BaseChunk() : base(){

    }
    public void SetActive(bool active){
        this.active = active;
    }
    public void FreeChunkMesh(){
        disposeStatus = DisposeState.FREE_MESH;
        TerraxelWorld.ChunkManager.DisposeChunk(this);
    }
    public void PoolChunk(){
        disposeStatus = DisposeState.POOL;
        TerraxelWorld.ChunkManager.DisposeChunk(this);
    }
    protected void RenderInstances(){
        if(!TerraxelWorld.renderGrass || (this is Chunk2D && depth != treeDepth)) return;
        for(int i = 0; i < 5; i++){
            instanceRenderers[i]?.Render();
        }
    }
    internal override void JobsReady()
    {
        if(renderBoundsData.IsCreated){
            boundSource = renderBoundsData.Value;
            renderBoundsData.Dispose();
        }
        RecalculateBounds();
        OnJobsReady();
    }
    protected virtual void OnJobsReady(){
        ApplyMesh();
        PushInstanceData();
    }
    protected void PushInstanceData(){
        if(this is Chunk2D && depth != treeDepth) return;
        for(int i = 0; i < 5; i++){
            instanceRenderers[i]?.propertyBlock.SetVector("_WorldPos", new float4(WorldPosition, 1));
            instanceRenderers[i]?.PushData(instanceDatas[i]);
        }
        instanceDatas.Dispose();
        instanceDatas = default;
    }
    public void ScheduleMeshUpdate(){
        propertyBlock.SetVector("_WorldPos", new float4(WorldPosition, 1));
        vertCount = 0;
        idxCount = 0;
        chunkState = ChunkState.DIRTY;
        genTime = Time.realtimeSinceStartup;
        if(this is Chunk3D || (this is Chunk2D && depth == treeDepth)){
            for(int i = 0; i < 5; i++){
                if(BiomesGenerated.Get(i).density > 0){
                    instanceDatas[i] = MemoryManager.GetInstancingData();
                }
            }
        }
        boundSource = new float3x2(new float3(ChunkManager.chunkResolution * depthMultiplier), 0f);
        renderBoundsData = new NativeReference<float3x2>(boundSource, Allocator.TempJob);
        OnScheduleMeshUpdate();
        
    }
    protected abstract void OnScheduleMeshUpdate();
    public abstract void RenderChunk();
    public abstract void ApplyMesh();
    protected abstract void OnFreeBuffers();
    public void FreeBuffers(){
        if(depth < treeDepth){
            for(int i = 0; i < 5; i++){
                instanceRenderers?[i]?.Dispose();
            }
        }
        OnFreeBuffers();
    }
    public virtual void OnMeshReady(){
        genTime = Time.realtimeSinceStartup - genTime;
        chunkState = ChunkState.READY;
        if(onMeshReady == OnMeshReadyAction.ALERT_PARENT){
            base.NotifyParentMeshReady();
        }else if(onMeshReady == OnMeshReadyAction.DISPOSE_CHILDREN){
            onMeshReady = OnMeshReadyAction.ALERT_PARENT;
            PruneChunksRecursive();
        }
    }
    public virtual void RefreshRenderState(bool refreshNeighbours = false){}
    
    public float3 WorldPosition{
        get{
            return (float3)region.center - new float3(ChunkManager.chunkResolution * depthMultiplier / 2);
        }
    }
    public override string ToString()
    {
        return "Chunk " + WorldPosition.ToString();
    }
    public void RecalculateBounds(){
        if(depth == TerraxelConstants.lodLevels -1 ){
            renderBounds = new Bounds(region.center, region.bounds);
        }else{
            renderBounds = new Bounds(WorldPosition + (boundSource.c0 + boundSource.c1) * 0.5f, boundSource.c1 - boundSource.c0);
        }
    }
}
