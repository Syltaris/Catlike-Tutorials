Shader "HashViz" {

	SubShader {
		CGPROGRAM
		#pragma surface ConfigureSurface Standard fullforwardshadows addshadow
		#pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
		#pragma editor_sync_compilation

		#pragma target 4.5
		
		#include "HashViz.hlsl"

		struct Input {
			float3 worldPos;
		};

		float _Smoothness;

		void ConfigureSurface (Input input, inout SurfaceOutputStandard surface) {
			surface.Albedo.rg = input.worldPos.xy * 0.5 + 0.5; // blue is going to be 0 since z is statically 0, so we ignore it
            surface.Smoothness = _Smoothness;
		}
		ENDCG
	}

	FallBack "Diffuse"
}