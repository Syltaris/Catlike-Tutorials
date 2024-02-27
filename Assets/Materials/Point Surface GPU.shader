Shader "Graph/Point Surface GPU" {
    Properties {
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }

	SubShader {
        CGPROGRAM

        #pragma surface ConfigureSurface Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
        #pragma editor_sync_compilation // This will force Unity to stall and immediately compile the shader right before it gets used the first time, avoiding the dummy shader.
        #pragma target 4.5

        /*
            But we should do this only for shader variants specifically compiled for procedural drawing. 
            This is the case when the UNITY_PROCEDURAL_INSTANCING_ENABLED macro label is defined
        */
        #include "PointGPU.hlsl"

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
