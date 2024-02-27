Shader "Graph/Point Surface GPU" {
    Properties {
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }

	SubShader {
        CGPROGRAM

        #pragma surface ConfigureSurface Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
        #pragma target 4.5

	    struct Input {
			float3 worldPos;
		};
	
        float _Smoothness;
		float _Step;

        /*
            But we should do this only for shader variants specifically compiled for procedural drawing. 
            This is the case when the UNITY_PROCEDURAL_INSTANCING_ENABLED macro label is defined
        */
        #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
            StructuredBuffer<float3> _Positions;
        #endif

        /*
            There is also a unity_WorldToObject matrix, which contains the inverse transformation, 
            used for transforming normal vectors. It is needed to correctly transform direction vectors 
            when a nonuniform deformation is applied. But as this doesn't apply to our graph we can ignore it. 
            We should tell this to our shaders though, by adding assumeuniformscaling to the instancing options pragma.
        */
        void ConfigureProcedural () {
            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                float3 position = _Positions[unity_InstanceID]; // We can access its identifier via unity_InstanceID, which is globally accessible.

                unity_ObjectToWorld = 0.0;
				unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);
				unity_ObjectToWorld._m00_m11_m22 = _Step;

            #endif

        }

		void ConfigureSurface (Input input, inout SurfaceOutputStandard surface) {
            surface.Albedo.rg = input.worldPos.xy * 0.5 + 0.5; // blue is going to be 0 since z is statically 0, so we ignore it
            surface.Smoothness = _Smoothness;
        }

        ENDCG
    }
    
    FallBack "Diffuse"
}
