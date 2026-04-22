// Runtime assembly is fine (no UnityEditor here)
using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public sealed class CreateSubAssetAttribute : PropertyAttribute
{
    public readonly Type baseType;
    public readonly string defaultName;
    public readonly bool allowAbstract;

    // baseType is the "root" type shown in the picker (and used to validate selection)
    public CreateSubAssetAttribute(Type baseType, string defaultName = null, bool allowAbstract = false)
    {
        this.baseType = baseType;
        this.defaultName = defaultName;
        this.allowAbstract = allowAbstract;
    }
}