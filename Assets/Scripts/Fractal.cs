using System;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;


public class Fractal : MonoBehaviour {

	[SerializeField, Range(1, 8)]
	int depth = 4;

    [SerializeField]
	Mesh mesh;

	[SerializeField]
	Material material;

    /*
    At minimum we need to know the direction and rotation of a part. We could keep track of those by storing them in arrays. 
    But instead of using separate arrays for vectors and quaternions we'll group them together, by creating a new FractalPart struct type. 
    This is done like defining a class, but with the struct keyword instead of class. 
    As we'll only need this type inside Fractal define it inside that class, along with its fields. Don't make it public, for the same reason.
    */
    struct FractalPart {
		public float3 direction, worldPosition;
		public quaternion rotation, worldRotation;
		public float spinAngle;
	}

	NativeArray<FractalPart>[] parts;
	NativeArray<float4x4>[] matrices;

	static float3[] directions = {
		up(), right(), left(), forward(), back()
	};

	static quaternion[] rotations = {
		quaternion.identity,
		quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
		quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
	};

	FractalPart CreatePart (int childIndex) => new FractalPart {
		direction = directions[childIndex],
		rotation = rotations[childIndex]
	};

	ComputeBuffer[] matricesBuffers;

	static readonly int matricesId = Shader.PropertyToID("_Matrices");

	static MaterialPropertyBlock propertyBlock;


	/*
	The convention is to prefix all interface types with an I which stands for interface, so the interface is named JobFor with an I prefix. 
	It's a job interface, specifically one that is used for functionality that runs inside for loops.
	*/
	[BurstCompile(CompileSynchronously = true)]
	struct UpdateFractalLevelJob : IJobFor {
		public float spinAngleDelta;
		public float scale;

		[ReadOnly]
		public NativeArray<FractalPart> parents;
		public NativeArray<FractalPart> parts;

		[WriteOnly]
		public NativeArray<float4x4> matrices;

		public void Execute (int i) {
			FractalPart parent = parents[i / 5];
			FractalPart part = parts[i];
			part.spinAngle += spinAngleDelta;
			part.worldRotation = mul(parent.worldRotation,
				mul(part.rotation, quaternion.RotateY(part.spinAngle))
			);
			part.worldPosition =
				parent.worldPosition +
				mul(parent.worldRotation, 1.5f * scale * part.direction);
			parts[i] = part;

			matrices[i] = float4x4.TRS(
				part.worldPosition, part.worldRotation, float3(scale)
			);
		}
	}

    void OnEnable () {
		parts = new NativeArray<FractalPart>[depth];
		matrices = new NativeArray<float4x4>[depth];
		matricesBuffers = new ComputeBuffer[depth];
		//parts.SetValue( new FractalPart[1], 0);

		int stride = 16 * 4; //  A 4×4 matrix has sixteen float values, so the stride of the buffers is sixteen times four bytes
        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5) { // go through roots depths
			parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
			matrices[i] = new NativeArray<float4x4>(length, Allocator.Persistent);
			matricesBuffers[i] = new ComputeBuffer(length, stride);
		}

		parts[0][0] = CreatePart(0);
        for (int li = 1; li < parts.Length; li++) {
            NativeArray<FractalPart> levelParts = parts[li];
			for (int fpi = 0; fpi < levelParts.Length; fpi += 5) {
				for (int ci = 0; ci < 5; ci++) {
					levelParts[fpi + ci] = CreatePart(ci);
				}
			}
        }

		propertyBlock ??= new MaterialPropertyBlock();

	}

	void OnDisable () {
		for (int i = 0; i < matricesBuffers.Length; i++) {
			matricesBuffers[i].Release();
			parts[i].Dispose();
			matrices[i].Dispose();
		}
		parts = null;
		matrices = null;
		matricesBuffers = null;
	}

	/*
	This also makes it possible to easily support changing the fractal depth via the inspector while in play mode,
	by adding an OnValidate method that simply invokes OnDisable and OnEnable after each other, resetting the fractal. 
	The OnValidate method gets invoked after a change has been made to the component via the inspector or an undo/redo action.
	*/
	void OnValidate () {
		if (parts != null && enabled) {
			OnDisable();
			OnEnable();
		}
	}

    void Update () {
		/*
		In Update we revert to the older approach of using a spin delta angle, which we then add to the root's spin angle. 
		The root's world rotation becomes equal to its configured rotation applied on top of a new rotation around the Y axis equal to its current spin angle.

		A side effect of creating our own transformation matrices is that our fractal now ignores the transformation of its game object. 
		We can fix this by incorporating the game object's rotation and position into the root object matrix in Update.
		*/
		float spinAngleDelta = 0.125f * PI * Time.deltaTime;

		FractalPart rootPart = parts[0][0];
		rootPart.spinAngle += spinAngleDelta;
		rootPart.worldRotation = mul(transform.rotation,
			mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle))
		);
		rootPart.worldPosition = transform.position;


		/*
		We can also apply the game object's scale. However, if a game object is part of a complex hierarchy 
		that includes nonuniform scales along with rotations it could be subject to a non-affine transformation that causes it to shear. 
		In this case it doesn't have a well-defined scale. For this reason Transform components do not have a simple world-space scale property. 
		They have a lossyScale property instead, to indicate that it might not be an exact affine scale. We'll simple use the X component of that scale, 
		ignoring any nonuniform scales.
		*/
		parts[0][0] = rootPart;
		float objectScale = transform.lossyScale.x;
		matrices[0][0] = float4x4.TRS(
			rootPart.worldPosition, rootPart.worldRotation, float3(objectScale)
		);

		float scale = objectScale;

		JobHandle jobHandle = default;
		for (int li = 1; li < parts.Length; li++) {
			scale *= 0.5f;

			jobHandle = new UpdateFractalLevelJob {
				spinAngleDelta = spinAngleDelta,
				scale = scale,
				parents = parts[li - 1],
				parts = parts[li],
				matrices = matrices[li]
			}.Schedule(parts[li].Length, jobHandle); // only the last jobHandle needs to be passed down and 'await'ed

		}
		jobHandle.Complete();

		var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);
		for (int i = 0; i < matricesBuffers.Length; i++) {
			ComputeBuffer buffer = matricesBuffers[i];
			buffer.SetData(matrices[i]);
			propertyBlock.SetBuffer(matricesId, buffer);
			Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, buffer.count, propertyBlock);
		}
	}
}