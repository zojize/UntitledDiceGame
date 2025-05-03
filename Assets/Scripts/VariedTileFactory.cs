using UnityEngine;

class VariedTileFactory : ITileFactory
{
    public (int Value, DieFaceType Type, Texture texture) GetTileOptions()
    {
        int value = Random.Range(1, 11);
        DieFaceType type = (DieFaceType)Random.Range(0, 3);
        var color = type switch
        {
          DieFaceType.Damage => "RED",
          DieFaceType.Heal => "GREEN",
          DieFaceType.Multiplier => "PURPLE",
          _ => throw new System.NotImplementedException(),
        };
        Texture texture = Resources.Load<Texture>($"Textures/Numbers/{color}_{value}");

        return (value, type, texture);
    }
}