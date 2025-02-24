using UnityEngine;
using UnityEngine.EventSystems;

public class BlueprintTile : MonoBehaviour, IPointerClickHandler
{
    public bool IsSelected;

    private Renderer _renderer;
    // private Material _outlineMaterial;

    public void Awake()
    {
        _renderer = GetComponent<Renderer>();
        // _outlineMaterial = Instantiate(Resources.Load<Material>("Materials/PlaneOutlineMaterial"));
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Clicked on " + name);
        IsSelected = !IsSelected;
        SetOutline(IsSelected);
    }

    public void SetTexture(Texture texture)
    {
        _renderer.material.mainTexture = texture;
    }


    private void SetOutline(bool enable)
    {
        _renderer.material.SetFloat("_OutlineEnabled", enable ? 1 : 0);
    }
}
