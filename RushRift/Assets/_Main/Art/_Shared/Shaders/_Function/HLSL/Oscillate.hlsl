#ifndef OSCILLATE_INCLUDED
#define OSCILLATE_INCLUDED

inline float oscillate01(const float time, const float speed)
{
    return sin(time * speed) * 0.5 + 0.5;
}

inline float oscillate_range(const float time, const float speed, const float min_value, const float max_value)
{
    return lerp(min_value, max_value, oscillate01(time, speed));
}

#endif