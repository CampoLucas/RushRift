#ifndef RADIAL_FILL_INCLUDED
#define RADIAL_FILL_INCLUDED

#ifndef PI
    #define PI 3.14159265359
#endif

inline float radial_mask(const float2 uv, const float fill_amount, const float origin, const int clockwise)
{
    const float2 p = uv - float2(0.5, 0.5);
float angle = atan2(p.y, p.x);

    angle = angle / (2.0 * PI);
    angle = frac(angle + 1.0);
    angle = frac(angle - origin + 1.0);

    const float final_angle = lerp(angle, 1.0 - angle, clockwise);
    // if (clockwise > 0.5)
    // {
    //     angle = 1.0 - angle;
    // }

    const float a = saturate(fill_amount);
    return step(final_angle, a);
}

#endif
