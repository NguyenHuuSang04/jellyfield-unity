using UnityEngine;
using JellyField.Core;

namespace JellyField.View
{
    public class JellyBlockView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer meshRenderer; 

        [Header("Jelly Materials Mapping")]
        [SerializeField] private Material purpleMat; 
        [SerializeField] private Material blueMat;   
        [SerializeField] private Material greenMat;  
        [SerializeField] private Material pinkMat;   

        // GHI NHỚ LOGIC CHO HỆ THỐNG QUÉT:
        [HideInInspector] [SerializeField] private Vector2Int localSlot;
        [HideInInspector] [SerializeField] private BlockColor blockColor;

        public Vector2Int LocalSlot
        {
            get => localSlot;
            set => localSlot = value;
        }

        public BlockColor BlockColor
        {
            get => blockColor;
            set => blockColor = value;
        }

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
}