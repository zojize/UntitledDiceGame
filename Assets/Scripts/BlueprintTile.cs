using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class BlueprintTile : MonoBehaviour, IPointerClickHandler, IDieFace
{
    public bool IsSelected { get; private set; }
    public Transform Up;
    public Transform Down;
    public Transform Left;
    public Transform Right;

    public DieFaceType Type { get; set; }
    public int Value { get; set; }

    public event Action<BlueprintTile> OnSelectionChangeEvent;

    private Renderer _renderer;
    // private Material _outlineMaterial;

    public void Awake()
    {
        _renderer = GetComponent<Renderer>();

        Up = transform.Find("Up");
        Down = transform.Find("Down");
        Left = transform.Find("Left");
        Right = transform.Find("Right");
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        // Debug.Log("Clicked on " + name);
        IsSelected = !IsSelected;
        OnSelectionChangeEvent?.Invoke(this);
        SetOutline(IsSelected);
    }

    public void PreventSelection()
    {
        IsSelected = false;
    }

    public void SetTexture(Texture texture)
    {
        _renderer.material.mainTexture = texture;
    }


    private void SetOutline(bool enable)
    {
        _renderer.material.SetVector("_OutlineColor", enable ? Color.yellow : Color.black);
    }
}
