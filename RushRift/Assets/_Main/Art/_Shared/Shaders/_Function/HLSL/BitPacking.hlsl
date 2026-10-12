#ifndef BIT_PACKING_INCLUDED
#define BIT_PACKING_INCLUDED

inline uint unpack_to_byte(const float value)
{
    return (uint)round(saturate(value) * 255.0);
}

inline float unpack_bit_8(const float packed, const uint index)
{
    return (unpack_to_byte(packed) >> index) & 1u;
}

inline float unpack_bit_24(const float3 packed, const uint index)
{
    const uint value = unpack_to_byte(packed[index / 8u]);
    return (value >> (index % 8u)) & 1u;
}

inline float unpack_bit_32(const float4 packed, const uint index)
{
    const uint value = unpack_to_byte(packed[index / 8u]);
    return (value >> (index % 8u)) & 1u;
}

inline float unpack_split_a(const float packed)
{
    return (unpack_to_byte(packed) >> 4u) / 15.0;
}

inline float unpack_split_b(const float packed)
{
    return (unpack_to_byte(packed) & 15u) / 15.0;
}

inline float unpack_split_6(const float3 packed, const uint index)
{
    const uint value = unpack_to_byte(packed[index / 2u]);
    const uint shift = (index % 2u == 0u) ? 4u : 0u;
    return ((value >> shift) & 15u) / 15.0;
}

inline float unpack_bit_8_float(const float packed, const float index)
{
    return unpack_bit_8(packed, (uint)index);
}

inline void unpack_bit_24_float(const float3 packed, const float index, out float Out)
{
    Out = unpack_bit_24(packed, (uint)index);
}

inline void unpack_split_2_float(const float packed, out float mask_a, out float mask_b)
{
    mask_a = unpack_split_a(packed);
    mask_b = unpack_split_b(packed);
}

inline void unpack_split_6_float(const float3 packed, const float index, out float Out)
{
    Out = unpack_split_6(packed, (uint)index);
}

#endif
