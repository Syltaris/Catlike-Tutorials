#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	StructuredBuffer<uint> _Hashes;
#endif

float4 _Config;

void ConfigureProcedural () {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		float v = floor(_Config.y * unity_InstanceID + 0.00001);	// divide ID into rows, epsilon to prevent float point offset errors
		float u = unity_InstanceID - _Config.x * v;
		
		unity_ObjectToWorld = 0.0;
		unity_ObjectToWorld._m03_m13_m23_m33 = float4(
			_Config.y * (u + 0.5) - 0.5, // x
			_Config.z * ((1.0 / 255.0) * (_Hashes[unity_InstanceID] >> 24) - 0.5), // y | shift right 24, scale to 0-1, offset by -1/2
			_Config.y * (v + 0.5) - 0.5, // z 
			1.0 // scale
		);
		unity_ObjectToWorld._m00_m11_m22 = _Config.y; // ?

		/*
			We then use the UV coordinates to place the instance on the XZ plane, 
			offset and scaled such that it remains inside the unit cube at the origin.
		*/
	#endif
}

/*
Also introduce a function that retrieves the hash and uses it to produce an RGB color. 
Initially make it a grayscale value that divides the hash by the resolution squared, thus going from black to white based on the hash index.
*/

float3 GetHashColor () {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		uint hash = _Hashes[unity_InstanceID];
		return (1.0 / 255.0) * float3(
			hash & 255,
			(hash >> 8) & 255,
			(hash >> 16) & 255
		); // eight least-significant bits of the hash in GetHashColor. This is done by combining the hash with binary 11111111 which is decimal 255, via the & bitwise AND operator.
	#else
		return 1.0;
	#endif
}

/*
Follow up with the shader graph function that we'll use to pass though the position and also output the color.
*/

void ShaderGraphFunction_float (float3 In, out float3 Out, out float3 Color) {
	Out = In;
	Color = GetHashColor();
}

void ShaderGraphFunction_half (half3 In, out half3 Out, out half3 Color) {
	Out = In;
	Color = GetHashColor();
}