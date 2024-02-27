using UnityEngine;


public class GPUGraph : MonoBehaviour {

	const int maxResolution = 1000;

    [SerializeField, Range(10, maxResolution)]
    int resolution = 10;

    [SerializeField]
	FunctionLibrary.FunctionName function;

    [SerializeField, Min(0f)]
	float functionDuration = 1f, transitionDuration = 1f;

	public enum TransitionMode { Cycle, Random }

	[SerializeField]
	TransitionMode transitionMode = TransitionMode.Cycle;
	
    float duration;
    bool transitioning;
	FunctionLibrary.FunctionName transitionFunction;

	[SerializeField]
	Material material;

	[SerializeField]
	Mesh mesh;

	[SerializeField]
	ComputeShader computeShader;

	ComputeBuffer positionsBuffer;


	static readonly int positionsId = Shader.PropertyToID("_Positions"),
		resolutionId = Shader.PropertyToID("_Resolution"),
		stepId = Shader.PropertyToID("_Step"),
		timeId = Shader.PropertyToID("_Time"),
		transitionProgressId = Shader.PropertyToID("_TransitionProgress");


	void OnEnable () {
		// gets invoked when the component is disabled, which also happens if the graph is destroyed and right before a hot reload
		positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * 4); // 3 floats * 4 bytes

		/*
			Next, always use the square of the max resolution for the amount of elements of the buffer. 
			This means that we'll always claim 12MB—roughly 11.44MiB—of GPU memory, no matter the graph resolution.
		*/
	}

	void OnDisable () {
		positionsBuffer.Release();
		/*
  			we won't use this specific object instance after this point it's a good idea to explicitly set the field to reference null. 
			This makes it possible for the object to be reclaimed by Unity's memory garbage collection process the next time it runs, 
			if our graph gets disabled or destroyed while in play mode.

			It will get released eventually if nothing holds a reference to the object, when the garbage collector reclaims it. 
			But when this happens is arbitrary. It's best to release it explicitly as soon as possible, to avoid clogging memory.
		*/
		positionsBuffer = null;
	}

	// proxies stuff to compute shader?
	void UpdateFunctionOnGPU () {
		float step = 2f / resolution;
		computeShader.SetInt(resolutionId, resolution);
		computeShader.SetFloat(stepId, step);
		computeShader.SetFloat(timeId, Time.time); 

		/*
			We also have to set the positions buffer, which doesn't copy any data but links the buffer to the kernel.
			This is done by invoking SetBuffer, which works like the other methods except that it requires an extra argument. 
			Its first argument is the index of the kernel function, because a compute shader can contain multiple kernels 
			and buffers can be linked to specific ones. We could get the kernel index by invoking FindKernel on the compute shader, 
			but our single kernel always has index zero so we can use that value directly.
		*/
		var kernelIndex = (int)function + (int)(transitioning ? transitionFunction : function) * FunctionLibrary.FunctionCount;
		computeShader.SetBuffer(kernelIndex, positionsId, positionsBuffer);
		/*
			After setting the buffer we can run our kernel, by invoking Dispatch on the compute shader with four integer parameters.
			The first is the kernel index and the other three are the amount of groups to run, again split per dimension. 
			Using 1 for all dimensions would mean only the first group of 8×8 positions gets calculated.

			Because of our fixed 8×8 group size the amount of groups we need in the X and Y dimensions is equal to the resolution divided by eight, rounded up. 
			We can do this by performing a float division and passing the result to Mathf.CeilToInt.
		*/
		int groups = Mathf.CeilToInt(resolution / 8f);
		computeShader.Dispatch(kernelIndex, groups, groups, 1); //  index of kernel function in FunctionLibrary compute shader

		material.SetBuffer(positionsId, positionsBuffer);
		material.SetFloat(stepId, step);
		/*
			Because this way of drawing doesn't use game objects Unity doesn't know where in the scene the drawing happens. 
			We have to indicate this by providing a bounding box as an additional argument. 
			This is an axis-aligned box that indicates the spatial bounds of whatever we're drawing.
			Unity uses this to determine whether the drawing can be skipped, because it ends up outside the field of view of the camera. 
			This is known as frustum culling. So instead of evaluating the bounds per point it now happens for the entire graph at once. 
			This is fine for our graph, as the idea is that we view it in its entirety.
			But points have a size as well, half of which could poke outside the bounds in all directions. So we should increase the bounds likewise.


			The final argument that we must provide to DrawMeshInstancedProcedural is how many instances should be drawn. 
			This should match the amount of elements in the positions buffer, which we can retrieve via its count property.
		*/
		var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
		Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution);

		/*
			Shouldn't we use DrawMeshInstancedIndirect?
			The DrawMeshInstancedIndirect method is useful for when you do not know how many instances to draw on the CPU side 
			and instead provide that information with a compute shader via a buffer.
		*/
	}
 
	void Update () {
		duration += Time.deltaTime;

        if (transitioning) {
	        if (duration >= transitionDuration) {
				duration -= transitionDuration;
				transitioning = false;
			}
        } else if (duration >= functionDuration) {
            duration -= functionDuration;
            transitioning = true;
			transitionFunction = function;
            PickNextFunction();
		}

		UpdateFunctionOnGPU();
    }

    void PickNextFunction () {
		function = transitionMode == TransitionMode.Cycle ?
			FunctionLibrary.GetNextFunctionName(function) :
			FunctionLibrary.GetRandomFunctionNameOtherThan(function);
	}

}