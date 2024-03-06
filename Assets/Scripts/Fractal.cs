using System;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = UnityEngine.Random;

using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;


public class Fractal : MonoBehaviour {

	[SerializeField, Range(3, 8)]
	int depth = 4;

    [SerializeField]
	Mesh mesh, leafMesh;

	[SerializeField]
	Material material;

    /*
    At minimum we need to know the direction and rotation of a part. We could keep track of those by storing them in arrays. 
    But instead of using separate arrays for vectors and quaternions we'll group them together, by creating a new FractalPart struct type. 
    This is done like defining a class, but with the struct keyword instead of class. 
    As we'll only need this type inside Fractal define it inside that class, along with its fields. Don't make it public, for the same reason.
    */
    struct FractalPart {
		public float3  worldPosition;
		public quaternion rotation, worldRotation;
		public float maxSagAngle, spinAngle, spinVelocity;
	}

	NativeArray<FractalPart>[] parts;
	NativeArray<float3x4>[] matrices;

	static quaternion[] rotations = {
		quaternion.identity,
		quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
		quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
	};

	FractalPart CreatePart (int childIndex) => new FractalPart {
		maxSagAngle = radians(Random.Range(maxSagAngleA, maxSagAngleB)),
		rotation = rotations[childIndex],
		spinVelocity =
			(Random.value < reverseSpinChance ? -1f : 1f) *
			radians(Random.Range(spinSpeedA, spinSpeedB))	
	};
	static MaterialPropertyBlock propertyBlock;

	ComputeBuffer[] matricesBuffers;

	static readonly int 
		matricesId = Shader.PropertyToID("_Matrices"),
		colorAId = Shader.PropertyToID("_ColorA"),
		colorBId = Shader.PropertyToID("_ColorB"),
		baseColorId = Shader.PropertyToID("_BaseColor"),
		sequenceNumbersId = Shader.PropertyToID("_SequenceNumbers");


	[SerializeField]
	Gradient gradientA, gradientB;

	[SerializeField]
	Color leafColorA, leafColorB;

	
	[SerializeField, Range(0f, 90f)]
	float maxSagAngleA = 15f, maxSagAngleB = 25f;

	[SerializeField, Range(0f, 90f)]
	float spinSpeedA = 20f, spinSpeedB = 25f;

	[SerializeField, Range(0f, 1f)]
	float reverseSpinChance = 0.25f;
	/*
	The convention is to prefix all interface types with an I which stands for interface, so the interface is named JobFor with an I prefix. 
	It's a job interface, specifically one that is used for functionality that runs inside for loops.
	*/
	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	struct UpdateFractalLevelJob : IJobFor {
		public float deltaTime;
		public float scale;

		[ReadOnly]
		public NativeArray<FractalPart> parents;
		public NativeArray<FractalPart> parts;

		[WriteOnly]
		public NativeArray<float3x4> matrices;

		public void Execute (int i) {
			FractalPart parent = parents[i / 5];
			FractalPart part = parts[i];
			part.spinAngle += part.spinVelocity * deltaTime;

			float3 upAxis = mul(mul(parent.worldRotation, part.rotation), up()); // parent * child then mask for up() vector
			float3 sagAxis = cross(up(), upAxis);
			float sagMagnitude = length(sagAxis);
			quaternion baseRotation;
			if (sagMagnitude > 0f) {
				sagAxis /= sagMagnitude;
				quaternion sagRotation =
					quaternion.AxisAngle(sagAxis, part.maxSagAngle * sagMagnitude);
				baseRotation = mul(sagRotation, parent.worldRotation);
			}
			else {
				baseRotation = parent.worldRotation;
			}
			part.worldRotation = mul(baseRotation,
				mul(part.rotation, quaternion.RotateY(part.spinAngle))
			);

			part.worldPosition =
				parent.worldPosition +
				mul(part.worldRotation, float3(0f, 1.5f * scale, 0f));
			parts[i] = part;

			float3x3 r = float3x3(part.worldRotation) * scale;
			matrices[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
		}
	}

	Vector4[] sequenceNumbers;

    void OnEnable () {
		parts = new NativeArray<FractalPart>[depth];
		matrices = new NativeArray<float3x4>[depth];
		matricesBuffers = new ComputeBuffer[depth];
		//parts.SetValue( new FractalPart[1], 0);

		int stride = 12 * 4; // strip non-variant row
		sequenceNumbers = new Vector4[depth];
        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5) { // go through roots depths
			parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
			matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
			matricesBuffers[i] = new ComputeBuffer(length, stride);
			sequenceNumbers[i] = new Vector4(Random.value, Random.value, Random.value, Random.value);
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
		sequenceNumbers = null;
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
		float3x3 r = float3x3(rootPart.worldRotation) * objectScale;
		matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.worldPosition);

		float scale = objectScale;
		float deltaTime = Time.deltaTime;
		rootPart.spinAngle += rootPart.spinVelocity * deltaTime;

		JobHandle jobHandle = default;
		for (int li = 1; li < parts.Length; li++) {
			scale *= 0.5f;

			jobHandle = new UpdateFractalLevelJob {				
				deltaTime = deltaTime,
				scale = scale,
				parents = parts[li - 1],
				parts = parts[li],
				matrices = matrices[li]
			}.ScheduleParallel(parts[li].Length, 5, jobHandle);
			 // only the last jobHandle needs to be passed down and 'await'ed

		}
		jobHandle.Complete();

		int leafIndex = matricesBuffers.Length - 1;
		var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);
		for (int i = 0; i < matricesBuffers.Length; i++) {
			ComputeBuffer buffer = matricesBuffers[i];
			buffer.SetData(matrices[i]);
			propertyBlock.SetBuffer(matricesId, buffer);
			propertyBlock.SetVector(sequenceNumbersId, sequenceNumbers[i]);

			Color colorA, colorB;
			Mesh instanceMesh;

			if (i == leafIndex) {
				colorA = leafColorA;
				colorB = leafColorB;
				instanceMesh = leafMesh;
			}
			else {
				float gradientInterpolator = i / (matricesBuffers.Length - 2f);
				colorA = gradientA.Evaluate(gradientInterpolator);
				colorB = gradientB.Evaluate(gradientInterpolator);
				instanceMesh = mesh;
			}
			propertyBlock.SetColor(colorAId, colorA);
			propertyBlock.SetColor(colorBId, colorB);

			Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, bounds, buffer.count, propertyBlock);
		}
	}
}