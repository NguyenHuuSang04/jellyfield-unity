using UnityEngine;

public class JellyBlockView : MonoBehaviour
{
    public MeshRenderer meshRenderer; 

    [Header("Jelly Materials Mapping")]
    public Material purpleMat; 
    public Material blueMat;   
    public Material greenMat;  
    public Material pinkMat;   

    // GHI NHỚ LOGIC CHO HỆ THỐNG QUÉT:
    [HideInInspector] public Vector2Int localSlot;
    [HideInInspector] public BlockColor blockColor;

    public void SetColorView(BlockColor color)
    {
        this.blockColor = color; // Lưu lại màu logic
        if (meshRenderer == null) return;

        switch (color)
        {
            case BlockColor.Purple: meshRenderer.material = purpleMat; break;
            case BlockColor.Blue: meshRenderer.material = blueMat; break;
            case BlockColor.Green: meshRenderer.material = greenMat; break;
            case BlockColor.Pink: meshRenderer.material = pinkMat; break;
        }
    }
}