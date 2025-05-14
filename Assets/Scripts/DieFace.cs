using UnityEngine;
public class DieFace : IDieFace
{
    public DieFaceType Type { get; set; }
    public int Value { get; set; }
    public Texture Texture { get; set; }

    public DieFace(DieFaceType type, int value, Texture texture)
    {
        Type = type;
        Value = value;
        Texture = texture;
    }
}
