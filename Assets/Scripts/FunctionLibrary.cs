using UnityEngine;

using static UnityEngine.Mathf;


public static class FunctionLibrary {

	public enum FunctionName { Wave, MultiWave, Ripple, Sphere, Torus }
	public delegate Vector3 Function (float u, float v, float t);


	readonly static Function[] functions = { Wave, MultiWave, Ripple, Sphere, Torus };


	public static Function GetFunction (FunctionName name) {
		return functions[(int)name];
	}

    // f(x,t)=sin(π(x+t))

	public static Vector3 Wave (float u, float v, float t) {
		Vector3 p;
		p.x = u;
		p.y = Sin(PI * (u + v + t));  // adding Z (fixed value) will vary the Y amount by fixed offsets
		p.z = v;
		return p;
	}

    // f(x,t)= sin(π(x+t)) + sin(2π(x+t)) / 2
	public static Vector3 MultiWave (float u, float v, float t) {
		Vector3 p;
		p.x = u;
		p.y = Sin(PI * (u + 0.5f * t));
		p.y += 0.5f * Sin(2f * PI * (v + t)); // adding Z (fixed value) will vary the Y amount by fixed offsets
		p.y += Sin(PI * (u + v + 0.25f * t)); // diagonal wave along XZ
		p.y *= 1f / 2.5f; // scale down to -1/1 bounds
		p.z = v;
		return p;
	}


    // We create it by making a sine wave move away from the origin, instead of always traveling in the same direction.
	
	public static Vector3 Ripple (float u, float v, float t) {
		float d = Sqrt(u * u + v * v);
		Vector3 p;
		p.x = u;
		p.y = Sin(PI * (4f * d - t));
		p.y /= 1f + 10f * d; // dampen amplitude with increasing distance
		p.z = v;
		return p;
	}

	public static Vector3 Sphere (float u, float v, float t) {
		// float r = 0.5f + 0.5f * Sin(PI * t); // uniform expansion/contraction
		// float r = 0.9f + 0.1f * Sin(8f * PI * u); // varied banding along Y axis (using v instead of u makes it along Z plane)
		float r = 0.9f + 0.1f * Sin(PI * (6f * u + 4f * v + t));
		// make radius smaller at the ends (top/bottom)
		float s = r * Cos(0.5f * PI * v);
		Vector3 p;
		p.x = s * Sin(PI * u);
		p.y = r * Sin(0.5f * PI * v);
		p.z = s * Cos(PI * u);
		return p;
	}

	public static Vector3 Torus (float u, float v, float t) {
		float r1 = 0.7f + 0.1f * Sin(PI * (6f * u + 0.5f * t));
		float r2 = 0.15f + 0.05f * Sin(PI * (8f * u + 4f * v + 2f * t));
		float s = r1 + r2 * Cos(PI * v); // r1 to move circles apart, r2 to control size of inner circle slices
		Vector3 p;
		p.x = s * Sin(PI * u);
		p.y = r2 * Sin(PI * v); // zoom into half circle part along y axis
		p.z = s * Cos(PI * u);
		return p;
	}
}