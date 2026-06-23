using UnityEngine;
using System.Collections.Generic;
using JellyField.Core;
using JellyField.Level;

namespace JellyField.View
{
    public class ClusterVisual : MonoBehaviour
    {
        [Header("Child Prefab")]
        [SerializeField] private GameObject jellyBlockPrefab; // Kéo file JellyBlock_Prefab vào đây

        [Header("Tùy Chỉnh Khe Hở")]
        [Range(0f, 0.2f)] 
        [SerializeField] private float gapPercent = 0.0f; // Mặc định bằng 0 để các khối khít sát nhau

        // Hàm tự động tính toán kích thước và vị trí thông minh lấp đầy 100% diện tích khay
        public void BuildCluster(ClusterData data)
        {
            if (jellyBlockPrefab == null || data == null || data.Blocks == null || data.Blocks.Count == 0) return;

            // Bốc trực tiếp cellSize từ GridManager để làm gốc tính toán hình học
            GridManager manager = Object.FindFirstObjectByType<GridManager>();
            float size = manager != null ? manager.CellSize : 1.4f;

            float maxSubBlockSize = size / 2f;
            float gap = size * gapPercent; 

            int count = data.Blocks.Count;

            // CẤU HÌNH COLLIDER DÀY DẶN: Giúp thao tác chạm bốc thạch siêu nhạy
            BoxCollider parentCollider = GetComponent<BoxCollider>();
            if (parentCollider != null)
            {
                parentCollider.center = Vector3.zero;
                parentCollider.size = new Vector3(size, 0.5f, size);
            }


            // TRƯỜNG HỢP 1: CÓ 1 KHỐI MÀU -> CHIẾM 100% DIỆN TÍCH KHAY
            if (count == 1)
            {
                var blockData = data.Blocks[0];
                int slotX = blockData.LocalSlot.x;
                int slotY = blockData.LocalSlot.y;

                GameObject blockObj = Instantiate(jellyBlockPrefab, transform);
                blockObj.transform.localPosition = Vector3.zero; 
                blockObj.transform.localRotation = Quaternion.identity;
                blockObj.transform.localScale = new Vector3(size - gap, 1f, size - gap);

                var blockView = blockObj.GetComponent<JellyBlockView>();
                if (blockView != null)
                {
                    // ÉP ĐỒNG BỘ: Ghi nhớ vị trí ô con trước khi nạp màu hiển thị
                    blockView.LocalSlot = new Vector2Int(slotX, slotY);
                    blockView.SetColorView(blockData.Color);
                }
            }

            // TRƯỜNG HỢP 2: CÓ 2 KHỐI MÀU -> MỖI KHỐI CHIẾM CHÍNH XÁC 50% DIỆN TÍCH 
            else if (count == 2)
            {
                var b0 = data.Blocks[0];
                var b1 = data.Blocks[1];
                bool isVerticalStack = (b0.LocalSlot.x == b1.LocalSlot.x);

                if (isVerticalStack)
                {
                    // XẾP TRÊN - DƯỚI: Khối dài nằm ngang
                    for (int i = 0; i < 2; i++)
                    {
                        var blockData = data.Blocks[i];
                        int slotX = blockData.LocalSlot.x;
                        int slotY = blockData.LocalSlot.y;

                        GameObject blockObj = Instantiate(jellyBlockPrefab, transform);
                        float posZ = (slotY == 0) ? -size / 4f : size / 4f;
                        
                        blockObj.transform.localPosition = new Vector3(0f, 0f, posZ);
                        blockObj.transform.localRotation = Quaternion.identity;
                        blockObj.transform.localScale = new Vector3(size - gap, 1f, maxSubBlockSize - gap);

                        var blockView = blockObj.GetComponent<JellyBlockView>();
                        if (blockView != null)
                        {
                            // ÉP ĐỒNG BỘ: Ghi nhớ vị trí ô con trước khi nạp màu hiển thị
                            blockView.LocalSlot = new Vector2Int(slotX, slotY);
                            blockView.SetColorView(blockData.Color);
                        }
                    }
                }
                else
                {
                    // XẾP TRÁI - PHẢI: Khối dài thẳng đứng
                    for (int i = 0; i < 2; i++)
                    {
                        var blockData = data.Blocks[i];
                        int slotX = blockData.LocalSlot.x;
                        int slotY = blockData.LocalSlot.y;

                        GameObject blockObj = Instantiate(jellyBlockPrefab, transform);
                        float posX = (slotX == 0) ? -size / 4f : size / 4f;
                        
                        blockObj.transform.localPosition = new Vector3(posX, 0f, 0f);
                        blockObj.transform.localRotation = Quaternion.identity;
                        blockObj.transform.localScale = new Vector3(maxSubBlockSize - gap, 1f, size - gap);

                        var blockView = blockObj.GetComponent<JellyBlockView>();
                        if (blockView != null)
                        {
                            // ÉP ĐỒNG BỘ: Ghi nhớ vị trí ô con trước khi nạp màu hiển thị
                            blockView.LocalSlot = new Vector2Int(slotX, slotY);
                            blockView.SetColorView(blockData.Color);
                        }
                    }
                }
            }

            // TRƯỜNG HỢP 3: CÓ 3 KHỐI MÀU -> 2 KHỐI 25%, 1 KHỐI TO CHIẾM 50%
            else if (count == 3)
            {
                bool[,] occupied = new bool[2, 2];
                foreach (var b in data.Blocks)
                {
                    occupied[Mathf.Clamp(b.LocalSlot.x, 0, 1), Mathf.Clamp(b.LocalSlot.y, 0, 1)] = true;
                }

                int emptyX = 0, emptyY = 0;
                for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++)
                        if (!occupied[x, y]) { emptyX = x; emptyY = y; }

                bool chooseHorizontalExpand = (emptyX == emptyY);

                foreach (var blockData in data.Blocks)
                {
                    int slotX = blockData.LocalSlot.x;
                    int slotY = blockData.LocalSlot.y;

                    float posX = (slotX == 0) ? -size / 4f : size / 4f;
                    float posZ = (slotY == 0) ? -size / 4f : size / 4f;
                    float scaleX = maxSubBlockSize - gap;
                    float scaleZ = maxSubBlockSize - gap;

                    if (chooseHorizontalExpand)
                    {
                        if (slotX == 1 - emptyX && slotY == emptyY)
                        {
                            posX = 0f;
                            scaleX = size - gap;
                        }
                    }
                    else
                    {
                        if (slotX == emptyX && slotY == 1 - emptyY)
                        {
                            posZ = 0f;
                            scaleZ = size - gap;
                        }
                    }

                    GameObject blockObj = Instantiate(jellyBlockPrefab, transform);
                    blockObj.transform.localPosition = new Vector3(posX, 0f, posZ);
                    blockObj.transform.localRotation = Quaternion.identity;
                    blockObj.transform.localScale = new Vector3(scaleX, 1f, scaleZ);

                    var blockView = blockObj.GetComponent<JellyBlockView>();
                    if (blockView != null)
                    {
                        // ÉP ĐỒNG BỘ: Ghi nhớ vị trí ô con trước khi nạp màu hiển thị
                        blockView.LocalSlot = new Vector2Int(slotX, slotY);
                        blockView.SetColorView(blockData.Color);
                    }
                }
            }

            // TRƯỜNG HỢP 4: CÓ 4 KHỐI MÀU -> CHIA ĐỀU MỖI KHỐI CHIẾM ĐÚNG 25% DIỆN TÍCH
            else if (count == 4)
            {
                foreach (var blockData in data.Blocks)
                {
                    int slotX = Mathf.Clamp(blockData.LocalSlot.x, 0, 1);
                    int slotY = Mathf.Clamp(blockData.LocalSlot.y, 0, 1);

                    float posX = (slotX == 0) ? -size / 4f : size / 4f;
                    float posZ = (slotY == 0) ? -size / 4f : size / 4f;

                    GameObject blockObj = Instantiate(jellyBlockPrefab, transform);
                    blockObj.transform.localPosition = new Vector3(posX, 0f, posZ);
                    blockObj.transform.localRotation = Quaternion.identity;
                    blockObj.transform.localScale = new Vector3(maxSubBlockSize - gap, 1f, maxSubBlockSize - gap);

                    var blockView = blockObj.GetComponent<JellyBlockView>();
                    if (blockView != null)
                    {
                        // ÉP ĐỒNG BỘ: Ghi nhớ vị trí ô con trước khi nạp màu hiển thị
                        blockView.LocalSlot = new Vector2Int(slotX, slotY);
                        blockView.SetColorView(blockData.Color);
                    }
                }
            }
        }
    }
}