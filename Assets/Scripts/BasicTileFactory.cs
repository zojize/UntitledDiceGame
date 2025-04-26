using UnityEngine;

class BasicTileFactory : ITileFactory
{
    public (int Value, DieFaceType Type, Texture texture) GetTileOptions()
    {
        int value = Random.Range(1, 7);
        DieFaceType type = DieFaceType.Damage;
        Texture texture = Resources.Load<Texture>($"Textures/dice_face_{value}");

        return (value, type, texture);
    }
}