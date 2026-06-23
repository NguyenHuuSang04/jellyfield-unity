using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JellyField.Core;

namespace JellyField.UI
{
    public class GoalItemUI : MonoBehaviour
    {
        public Image iconImage;           // Gắn Component Image hiển thị hình thạch vào đây
        public TextMeshProUGUI countText; // Gắn Component TextMeshPro hiển thị số lượng vào đây

        [Header("Jelly Sprites Mapping")]
        public Sprite purpleSprite; 
        public Sprite blueSprite;   
        public Sprite greenSprite;  
        public Sprite pinkSprite;   

        // Hàm API nạp dữ liệu hình ảnh và số lượng mục tiêu tự động
        public void SetupGoal(BlockColor color, int targetCount)
        {
            // 1. Cập nhật số lượng hiển thị (Ví dụ: "x 4")
            if (countText != null) countText.text = $"x{targetCount}";

            // 2. Gán đúng hình ảnh Sprite căng mọng theo màu sắc cấu hình
            if (iconImage != null)
            {
                switch (color)
                {
                    case BlockColor.Purple: iconImage.sprite = purpleSprite; break;
                    case BlockColor.Blue:   iconImage.sprite = blueSprite; break;
                    case BlockColor.Green:  iconImage.sprite = greenSprite; break;
                    case BlockColor.Pink:   iconImage.sprite = pinkSprite; break;
                    default: iconImage.sprite = null; break;
                }
                
                // Đảm bảo màu hiển thị của Image được reset về màu trắng gốc để không bị ám màu cũ
                iconImage.color = Color.white;
            }
        }
    }
}