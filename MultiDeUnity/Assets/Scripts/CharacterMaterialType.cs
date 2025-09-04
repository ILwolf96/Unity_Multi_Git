using UnityEngine;

public enum CharacterMaterialType : byte // why go higher then byte if that gets the job done
{
    None = 0,
    PlayerGreen = 1,
    PlayerLight = 2,
    PlayerYellow = 4,
    PlayerPurple = 3,
    PlayerPink = 5,
    PlayerOrange = 6,
    PlayerRed = 7,
    PlayerDark = 8,
    PlayerCyan = 9,
    PlayerBlue = 10,
}

[System.Serializable]
public struct MaterialMapping
{
    public CharacterMaterialType Type;
    public Material Material;
}