using System;
using System.Collections.Generic;
using Assets.Resources.Generated;
using Assets.Scripts.TerrainGeneration;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.Managers
{
	// todo remove
	public class MemoryManager
	{
		public const int assumedVertexCount = 7000;
		public const int assumedInstanceCount = 200;
		public const int densityMapLength = ChunkManager.chunkResolution * ChunkManager.chunkResolution * ChunkManager.chunkResolution;
		public const int initialPoolSize = 100;
		public const int maxChunkCount = 512;

		public static int maxConcurrentOperations = SystemInfo.processorCount - 2;
		private static readonly MemoryAllocation<MeshData> meshDatas = new();
		private static readonly MemoryAllocation<SimpleMeshData> simpleMeshDatas = new();
		private static MemoryAllocation<TempBuffer> vertexIndexBuffers = new();
		private static readonly MemoryAllocation<NativeList<InstanceData>> instancingDatas = new();
		private static readonly MemoryAllocation<ComputeBuffer> instancingBuffers = new();
		private static Queue<NativeArray<sbyte>> freeDensityMaps;
		private static NativeArray<sbyte> densityMap;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
		private static AtomicSafetyHandle densitySafetyHandle;
#endif
		private static readonly List<NativeArray<int2>> meshStarts = new();

		public static void Init()
		{
			for (var i = 0; i < initialPoolSize; i++)
			{
				AllocateMeshData();
				AllocateInstancingData();
				AllocateSimpleMeshData();
			}

			AllocateDensityMaps();
			AllocateTempBuffers();
		}

		private static void AllocateInstancingData()
		{
			instancingDatas.Enqueue(new NativeList<InstanceData>(4500, Allocator.Persistent), true);
		}

		public static void AllocateSimpleMeshData()
		{
			var vertBuffer = new NativeArray<VertexData>(Chunk2D.vertexCount, Allocator.Persistent);
			var indexBuffer = new NativeArray<ushort>(Chunk2D.indexCount, Allocator.Persistent);
			var heightMap = new NativeArray<float>(4489, Allocator.Persistent);
			var data = new SimpleMeshData();
			data.vertexBuffer = vertBuffer;
			data.indexBuffer = indexBuffer;
			data.heightMap = heightMap;
			simpleMeshDatas.Enqueue(data, true);
		}

		private static void AllocateTempBuffers()
		{
			vertexIndexBuffers = new MemoryAllocation<TempBuffer>();
			for (var i = 0; i < maxConcurrentOperations; i++)
			{
				var buf1 = new NativeArray<ReuseCell>(densityMapLength, Allocator.Persistent);
				var buf2 = new NativeArray<CellIndices>(ChunkManager.chunkResolution * ChunkManager.chunkResolution * 6, Allocator.Persistent);
				vertexIndexBuffers.Enqueue(new TempBuffer(buf1, buf2), true);
			}
		}

		private static void AllocateDensityMaps()
		{
			freeDensityMaps = new Queue<NativeArray<sbyte>>();
			densityMap = new NativeArray<sbyte>(densityMapLength * TerraxelConstants.densityMapCount, Allocator.Persistent);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			densitySafetyHandle = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(densityMap);
#endif
			for (var i = 0; i < TerraxelConstants.densityMapCount; i++)
			{
				var densities = densityMap.GetSubArray(i * densityMapLength, densityMapLength);
				freeDensityMaps.Enqueue(densities);
			}
		}

		public static ComputeBuffer GetInstancingBuffer(int dataLength)
		{
			var buf = new ComputeBuffer(dataLength, sizeof(float) * 16, ComputeBufferType.Structured);
			instancingBuffers.Enqueue(buf, true);
			return buf;
		}

		private static void AllocateMeshData()
		{
			var verts = new NativeList<TransitionVertexData>(assumedVertexCount, Allocator.Persistent);
			var indices = new NativeList<ushort>(assumedVertexCount, Allocator.Persistent);
			//var grassPositions = new NativeList<Matrix4x4>(grassAmount, Allocator.Persistent);
			meshDatas.Enqueue(new MeshData(verts, indices), true);
		}

		public static MeshData GetMeshData()
		{
			if (meshDatas.Count == 0) AllocateMeshData();
			return meshDatas.Dequeue();
		}

		public static NativeList<InstanceData> GetInstancingData()
		{
			if (instancingDatas.Count == 0) AllocateInstancingData();
			return instancingDatas.Dequeue();
		}

		public static SimpleMeshData GetSimpleMeshData()
		{
			if (simpleMeshDatas.Count == 0) AllocateSimpleMeshData();
			return simpleMeshDatas.Dequeue();
		}

		public static TempBuffer GetVertexIndexBuffer()
		{
			if (vertexIndexBuffers.Count == 0) 
				throw new Exception("No free vertex index buffer available", new InvalidOperationException());
			var thing = vertexIndexBuffers.Dequeue();
			return thing;
		}

		public static NativeArray<sbyte> GetDensityMap()
		{
			if (freeDensityMaps.Count == 0) throw new Exception("No free density map available", new InvalidOperationException());
			var thing = freeDensityMaps.Dequeue();
			return thing;
		}

		public static NativeArray<int2> GetMeshCounterArray()
		{
			var meshStart = new NativeArray<int2>(7, Allocator.Persistent);
			meshStarts.Add(meshStart);
			return meshStart;
		}

		public static void ReturnMeshData(MeshData data)
		{
			ClearArray(data.indexBuffer.AsArray(), data.indexBuffer.Length);
			data.indexBuffer.Length = 0;
			data.indexBuffer.Capacity = assumedVertexCount;
			ClearArray(data.vertexBuffer.AsArray(), data.vertexBuffer.Length);
			data.vertexBuffer.Length = 0;
			data.vertexBuffer.Capacity = assumedVertexCount;
			meshDatas.Enqueue(data, false);
		}

		public static void ReturnSimpleMeshData(SimpleMeshData data)
		{
			ClearArray(data.indexBuffer, data.indexBuffer.Length);
			simpleMeshDatas.Enqueue(data, false);
		}

		public static void ReturnInstanceData(NativeList<InstanceData> data)
		{
			ClearArray(data.AsArray(), data.Length);
			data.Length = 0;
			data.Capacity = 4500;
			instancingDatas.Enqueue(data, false);
		}

		public static void ReturnDensityMap(NativeArray<sbyte> map, bool assignSafetyHandle = false)
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			if (assignSafetyHandle)
			{
				NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref map, densitySafetyHandle);
			}
#endif
			freeDensityMaps.Enqueue(map);
		}

		public static void ReturnVertexIndexBuffer(TempBuffer buffer)
		{
			if (buffer.vertexIndices == default) throw new Exception("Tried to return invalid buffer", new InvalidCastException());
			ClearArray(buffer.transitionVertexIndices, buffer.transitionVertexIndices.Length);
			ClearArray(buffer.vertexIndices, buffer.vertexIndices.Length);
			vertexIndexBuffers.Enqueue(buffer, false);
		}

		public static int GetFreeVertexIndexBufferCount()
		{
			return vertexIndexBuffers.Count;
		}

		public static int GetFreeDensityMapCount()
		{
			return freeDensityMaps.Count;
		}

		public static void Dispose()
		{
			meshDatas.Dispose();
			vertexIndexBuffers.Dispose();
			instancingDatas.Dispose();
			foreach (var buffer in meshStarts) buffer.Dispose();

			simpleMeshDatas.Dispose();
			//SimpleMeshData.indices.Dispose();
			densityMap.Dispose();
			instancingBuffers.Dispose();
		}

		public static unsafe void ClearArray<T>(NativeArray<T> to_clear, int length) where T : struct
		{
			UnsafeUtility.MemClear(
				NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(to_clear),
				UnsafeUtility.SizeOf<T>() * length);
		}

		private class MemoryAllocation<T> : IDisposable where T : IDisposable
		{
			private readonly List<T> allInstances;

			private readonly Queue<T> freeInstances;

			public MemoryAllocation()
			{
				freeInstances = new Queue<T>();
				allInstances = new List<T>();
			}

			public int Count => freeInstances.Count;

			public void Dispose()
			{
				foreach (var buffer in allInstances) buffer.Dispose();
			}

			public void Enqueue(T t, bool newInstance)
			{
				freeInstances.Enqueue(t);
				if (newInstance)
					allInstances.Add(t);
			}

			public T Dequeue()
			{
				return freeInstances.Dequeue();
			}
		}
	}
}