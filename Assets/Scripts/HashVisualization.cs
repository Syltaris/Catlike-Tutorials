using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

using static Unity.Mathematics.math;

public class HashVisualization : MonoBehaviour
{
    static int hashesId = Shader.PropertyToID("_Hashes"),
        configId = Shader.PropertyToID("_Config");

    [SerializeField]
    Mesh instanceMesh;

    [SerializeField]
    Material material;

    [SerializeField, Range(1, 512)]
    int resolution = 16;

    [SerializeField]
    int seed;

    [SerializeField, Range(-2f, 2f)]
    float verticalOffset = 1f;

    NativeArray<uint> hashes;

    ComputeBuffer hashesBuffer;

    MaterialPropertyBlock propertyBlock;

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor
    {
        [WriteOnly]
        public NativeArray<uint> hashes; // memory shared by jobs?

        public int resolution;
        public float invResolution;

        public SmallXXHash hash;

        public void Execute(int i)
        {
            int v = (int)floor(invResolution * i + 0.00001f); // int divs cannot be vectorized so aren't used here
            int u = i - resolution * v - resolution / 2;
            // To demonstrate that our hash function also works for negative coordinates, subtract half the resolution from U and V in HashJob.Execute.
            v -= resolution / 2;

            u *= 2;
            v *= 2;

            hashes[i] = hash.Eat(u).Eat(v); // (uint)(frac(u * v * 0.381f) * 255f); // i * 0.381f | weyl sequence
        }
    }

    /*
        We need to both multiply with and divide by the resolution in the shader,
        so store the resolution and its reciprocal in the first two components of a configuration vector.
    */
    void OnEnable()
    {
        int length = resolution * resolution; // square grid
        hashes = new NativeArray<uint>(length, Allocator.Persistent);
        hashesBuffer = new ComputeBuffer(length, 4);

        // iterator parallelism
        new HashJob
        {
            hashes = hashes, // passes in shared buffer here
            resolution = resolution,
            invResolution = 1f / resolution,
            hash = SmallXXHash.Seed(seed)
        }
            .ScheduleParallel(hashes.Length, resolution, default)
            .Complete();

        hashesBuffer.SetData(hashes);

        propertyBlock ??= new MaterialPropertyBlock(); // ??= init with RHS if null
        propertyBlock.SetBuffer(hashesId, hashesBuffer);
        propertyBlock.SetVector(
            configId,
            new Vector4(resolution, 1f / resolution, verticalOffset / resolution)
        );
    }

    void OnDisable()
    {
        hashes.Dispose();
        hashesBuffer.Release();
        hashesBuffer = null; // for OnValidate logic
    }

    void OnValidate()
    {
        if (hashesBuffer != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    void Update()
    {
        Graphics.DrawMeshInstancedProcedural(
            instanceMesh,
            0,
            material,
            new Bounds(Vector3.zero, Vector3.one), // bounding volume???
            hashes.Length,
            propertyBlock
        );
    }
}
