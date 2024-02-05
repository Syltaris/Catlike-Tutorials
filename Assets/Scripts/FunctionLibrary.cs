using UnityEngine;

using static UnityEngine.Mathf;


public static class FunctionLibrary {

	public enum FunctionName { Wave, MultiWave, Ripple }
	public delegate float Function (float x, float z, float t);


	readonly static Function[] functions = { Wave, MultiWave, Ripple };


	public static Function GetFunction (FunctionName name) {
		return functions[(int)name];
	}

    // f(x,t)=sin(π(x+t))

	public static float Wave (float x, float z, float t) {
		return Sin(PI * (x + z +  t)); // adding Z (fixed value) will vary the Y amount by fixed offsets
	}

    // f(x,t)= sin(π(x+t)) + sin(2π(x+t)) / 2
	public static float MultiWave (float x, float z, float t) {
        float y = Sin(PI * (x + t));
		y += Sin(2f * PI * (x + z + t)) * (1f / 2f); // adding Z (fixed value) will vary the Y amount by fixed offsets
		y += Sin(PI * (x + z + 0.25f * t)); // diagonal wave along XZ
		return y * (1f / 2.5f);	// scale down to -1/1 bounds
    }


    // We create it by making a sine wave move away from the origin, instead of always traveling in the same direction.
    public static float Ripple (float x, float z, float t) {
		float d =  Sqrt(x * x + z * z); // Abs(x);
		float y = Sin(PI * (4f * d - t));
		return y / (1f + 10f * d); // dampen amplitude with increasing distance
	}
}