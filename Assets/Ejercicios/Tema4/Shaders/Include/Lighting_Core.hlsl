#ifndef LIGHTING_CORE_G1 //Preprocessor directive
#define LIGHTING_CORE_G1

void GetMainLight_float(float3 positionWS, out float3 directionWS, out float3 color, out float shadowAttenuation)
{
    directionWS = float3(1, 1, -1);
    color = 1;
    shadowAttenuation = 1;
}

#endif