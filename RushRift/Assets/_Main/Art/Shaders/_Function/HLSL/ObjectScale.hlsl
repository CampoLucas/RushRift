
#ifndef OBJECT_SCALE_INCLUDED
#define OBJECT_SCALE_INCLUDED

float3 get_object_world_scale()
{
    // unity_ObjectToWorld is a 4x4 matrix. The first 3 columns contain the
    // transformed basis vectors (with scale baked in).
    const float3 x_axis = float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20);
    const float3 y_axis = float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21);
    const float3 z_axis = float3(unity_ObjectToWorld._m02, unity_ObjectToWorld._m12, unity_ObjectToWorld._m22);

    float sx = length(x_axis);
    float sy = length(y_axis);
    float sz = length(z_axis);

    return float3(sx, sy, sz);
}

// If you want it as three separate floats for Amplify convenience:
void get_object_world_scale_float(out float sx, out float sy, out float sz)
{
    float3 s = get_object_world_scale();
    sx = s.x;
    sy = s.y;
    sz = s.z;
}

#endif