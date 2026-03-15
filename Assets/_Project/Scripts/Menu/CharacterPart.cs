using UnityEngine;
using System;

/// <summary>
/// Defines what body parts can be customized.
/// Add new types here as needed (Accessories, Backpack, etc.).
/// </summary>
public enum CharacterPartType
{
    Body,
    Head,
    Hair,
    Hands,
    Feet,
    Accessory1,
    Accessory2
}

/// <summary>
/// Single character part definition (mesh + material + enabled state).
/// Used in PlayerClassConfig to define a class's appearance.
/// </summary>
[Serializable]
public class CharacterPart
{
    [Tooltip("Which part of the character this affects")]
    public CharacterPartType partType;

    [Tooltip("Mesh to apply (null = keep current)")]
    public Mesh mesh;

    [Tooltip("Material to apply (null = keep current)")]
    public Material material;

    [Tooltip("Is this part visible/enabled?")]
    public bool enabled = true;

    public CharacterPart(CharacterPartType type)
    {
        partType = type;
        enabled = true;
    }
}