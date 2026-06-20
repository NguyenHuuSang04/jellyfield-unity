using UnityEngine;

public class JellyBlockView : MonoBehaviour
{
    public MeshRenderer meshRenderer; // Kéo thành phần MeshRenderer của khối Cube vào đây

    [Header("Jelly Materials Mapping")]
    public Material purpleMat; // Kéo file Mat_Jelly_Purple vào đây
    public Material blueMat;   // Kéo file Mat_Jelly_Blue vào đây
    public Material greenMat;  // Kéo file Mat_Jelly_Green vào đây
    public Material pinkMat;   // Kéo file Mat_Jelly_Pink vào đây

    // Hàm API để GridManager nạp màu khi sinh khối thạch
    public void SetColorView(BlockColor color)
    {
        if (meshRenderer == null) return;

        switch (color)
        {
            case BlockColor.Purple:
                meshRenderer.material = purpleMat;
                break;
            case BlockColor.Blue:
                meshRenderer.material = blueMat;
                break;
            case BlockColor.Green:
                meshRenderer.material = greenMat;
                break;
            case BlockColor.Pink:
                meshRenderer.material = pinkMat;
                break;
        }
    }
}