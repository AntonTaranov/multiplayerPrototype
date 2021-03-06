Shader "Mobile/Transparent/Vertex Color" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
}

Category {
	Tags {"Queue"="Transparent+1" "IgnoreProjector"="True" "RenderType"="Transparent"}
	ZWrite Off
	//Alphatest Greater 0
	Blend SrcAlpha OneMinusSrcAlpha 

	SubShader {
		Material {
			Diffuse [_Color]
			Ambient [_Color]
		}
		Pass {
			ColorMaterial AmbientAndDiffuse 
			Fog { Mode Off }
			Lighting Off
        	SetTexture [_MainTex] {
            Combine texture * primary, texture * primary
        }
        SetTexture [_MainTex] {
            constantColor [_Color]
            Combine previous * constant DOUBLE, previous * constant
        }  
		}
	} 
}
}