using UnityEngine;
using System.Collections.Generic;

public class ClusterVisual : MonoBehaviour
{
    [Header("Child Prefab")]
    public GameObject jellyBlockPrefab; // Kéo file JellyBlock_Prefab vào đây

    [Header("Tùy Chỉnh Khe Hở")]
    [Range(0f, 0.2f)] 
    public float gapPercent = 0.02f; // Cho phép kéo thanh trượt ngoài Inspector để chỉnh khe hở

    // Hàm tự động tính toán kích thước và vị trí thông minh lấp đầy 100% diện tích khay
    public void BuildCluster(ClusterData data)
    {
        if (jellyBlockPrefab == null || data == null || data.Blocks == null || data.Blocks.Count == 0) return;

        // Bốc trực tiếp cellSize (1.4f) từ GridManager để các khối con co giãn khít khịt với ô sàn
        GridManager manager = Object.FindFirstObjectByType<GridManager>();
        float size = manager != null ? manager.cellSize : 1.4f;

        // Định nghĩa kích thước vùng chứa tối đa
        float totalSize = size * 0.96f; 
        float halfSize = totalSize / 2f;
        float gap = size * gapPercent; 

        int count = data.Blocks.Count;

        // ===================================================================
        // TRƯỜNG HỢP 1: CÓ 1 KHỐI MÀU -> CHIẾM 100% DIỆN TÍCH KHAY
        // ===================================================================
        if (count == 1)
        {
            var blockData = data.Blocks[0];
            GameObject blockObj = Instantiate(jellyBlockPrefab, transform);
            blockObj.transform.localPosition = Vector3.zero; 
            blockObj.transform.localRotation = Quaternion.identity;
            blockObj.transform.localScale = new Vector3(totalSize, 1f, totalSize);

            var blockView = blockObj.GetComponent<JellyBlockView>();
            if (blockView != null) blockView.SetColorView(blockData.Color);
        }
        // ===================================================================
        // TRƯỜNG HỢP 2: CÓ 2 KHỐI MÀU -> MỖI KHỐI CHIẾM CHÍNH XÁC 50% DIỆN TÍCH
        // ===================================================================
        else if (count == 2)
        {
            var b0 = data.Blocks[0];
            var b1 = data.Blocks[1];
            bool isVerticalStack = (b0.LocalSlot.x == b1.LocalSlot.x);

            if (isVerticalStack)
            {
                for (int i = 0; i < 2; i++)
                {
                    var blockData = data.Blocks[i];
                    GameObject blockObj = Instantiate(jellyBlockPrefab, transform);
                    float posZ = (blockData.LocalSlot.y == 0) ? -size / 4f : size / 4f;
                    
                    blockObj.transform.localPosition = new Vector3(0f, 0f, posZ);
                    blockObj.transform.localRotation = Quaternion.identity;
                    blockObj.transform.localScale = new Vector3(totalSize, 1f, halfSize - gap);

                    var blockView = blockObj.GetComponent<JellyBlockView>();
                    if (blockView != null) blockView.SetColorView(blockData.Color);
                }
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    var blockData = data.Blocks[i];
                    GameObject blockObj = Instantiate(jellyBlockPrefab, transform);
                    float posX = (blockData.LocalSlot.x == 0) ? -size / 4f : size / 4f;
                    
                    blockObj.transform.localPosition = new Vector3(posX, 0f, 0f);
                    blockObj.transform.localRotation = Quaternion.identity;
                    blockObj.transform.localScale = new Vector3(halfSize - gap, 1f, totalSize);

                    var blockView = blockObj.GetComponent<JellyBlockView>();
                    if (blockView != null) blockView.SetColorView(blockData.Color);
                }
            }
        }
        // ===================================================================
        // FIX LỖI TRƯỜNG HỢP 3: CÓ 3 KHỐI MÀU -> 2 KHỐI 25%, CHỈ ĐÚNG 1 KHỐI 50%
        // ===================================================================
        else if (count == 3)
        {
            // Bước 1: Xác định tọa độ ô trống duy nhất trong khay 2x2
            bool[,] occupied = new bool[2, 2];
            foreach (var b in data.Blocks)
            {
                occupied[Mathf.Clamp(b.LocalSlot.x, 0, 1), Mathf.Clamp(b.LocalSlot.y, 0, 1)] = true;
            }

            int emptyX = 0, emptyY = 0;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    if (!occupied[x, y]) { emptyX = x; emptyY = y; }
                }
            }

            // Tạo luật thông minh: Nếu ô trống nằm chéo nhau, đổi hướng domino để tạo sự đa dạng hình dáng
            bool chooseHorizontalExpand = (emptyX == emptyY);

            // Bước 2: Sinh khối và áp dụng scale loại trừ (chỉ cho phép 1 khối duy nhất nở to)
            foreach (var blockData in data.Blocks)
            {
                int slotX = blockData.LocalSlot.x;
                int slotY = blockData.LocalSlot.y;

                // Tọa độ gốc mặc định của ô 25% (nằm góc vuông nhỏ)
                float posX = (slotX == 0) ? -size / 4f : size / 4f;
                float posZ = (slotY == 0) ? -size / 4f : size / 4f;
                float scaleX = halfSize - gap;
                float scaleZ = halfSize - gap;

                if (chooseHorizontalExpand)
                {
                    // CHỈ kéo dài khối hàng xóm NẰM NGANG vào ô trống (ví dụ khối màu xanh dương)
                    if (slotX == 1 - emptyX && slotY == emptyY)
                    {
                        posX = 0f; // Căn giữa lại trục X để phủ đều sang ô trống
                        scaleX = totalSize; // Dài ra chiếm 50% diện tích nằm ngang
                    }
                }
                else
                {
                    // CHỈ kéo dài khối hàng xóm NẰM DỌC vào ô trống
                    if (slotX == emptyX && slotY == 1 - emptyY)
                    {
                        posZ = 0f; // Căn giữa lại trục Z để phủ đều dọc
                        scaleZ = totalSize; // Dài ra chiếm 50% diện tích nằm dọc
                    }
                }

                GameObject blockObj = Instantiate(jellyBlockPrefab, transform);
                blockObj.transform.localPosition = new Vector3(posX, 0f, posZ);
                blockObj.transform.localRotation = Quaternion.identity;
                blockObj.transform.localScale = new Vector3(scaleX, 1f, scaleZ);

                var blockView = blockObj.GetComponent<JellyBlockView>();
                if (blockView != null) blockView.SetColorView(blockData.Color);
            }
        }
        // ===================================================================
        // TRƯỜNG HỢP 4: CÓ 4 KHỐI MÀU -> CHIA ĐỀU MỖI KHỐI CHIẾM ĐÚNG 25%
        // ===================================================================
        else if (count == 4)
        {
            foreach (var blockData in data.Blocks)
            {
                GameObject blockObj = Instantiate(jellyBlockPrefab, transform);
                int slotX = Mathf.Clamp(blockData.LocalSlot.x, 0, 1);
                int slotY = Mathf.Clamp(blockData.LocalSlot.y, 0, 1);

                float posX = (slotX == 0) ? -size / 4f : size / 4f;
                float posZ = (slotY == 0) ? -size / 4f : size / 4f;

                blockObj.transform.localPosition = new Vector3(posX, 0f, posZ);
                blockObj.transform.localRotation = Quaternion.identity;
                blockObj.transform.localScale = new Vector3(halfSize - gap, 1f, halfSize - gap);

                var blockView = blockObj.GetComponent<JellyBlockView>();
                if (blockView != null) blockView.SetColorView(blockData.Color);
            }
        }
    }
}