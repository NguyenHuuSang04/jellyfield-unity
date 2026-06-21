using UnityEngine;
using System.Collections.Generic;

public class DraggableGroup : MonoBehaviour
{
    private Camera mainCamera;
    private Vector3 offset;
    private bool isDragging = false;
    private Vector3 originalPosition;
    private float dragHeightOffset = 0.5f;

    [Header("Grid Snapping")]
    private GridManager gridManager;
    public float snapThreshold = 1.5f; 

    private int mySlotIndex = -1;
    private DockManager myDockManager;

    void Start()
    {
        mainCamera = Camera.main;
        originalPosition = transform.position;
        gridManager = Object.FindFirstObjectByType<GridManager>();
    }

    public void SetSlotInfo(int slotIndex, DockManager dock)
    {
        this.mySlotIndex = slotIndex;
        this.myDockManager = dock;
    }

    void OnMouseDown()
    {
        isDragging = true;
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        offset = transform.position - mouseWorldPos;

        if (myDockManager != null && mySlotIndex != -1)
        {
            myDockManager.NotifyDragStarted(mySlotIndex);
        }
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 targetPos = mouseWorldPos + offset;
        targetPos.y = originalPosition.y + dragHeightOffset; 
        
        transform.position = targetPos;
    }

    void OnMouseUp()
    {
        isDragging = false;

        // XỬ LÝ SNAP VÀ INJECT DỮ LIỆU VÀO LƯỚI NGẦM
        if (TrySnapAndInjectData())
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.dropSound);
            
            // Chạy hiệu ứng nảy dập dình tại tâm ô lưới
            JellyJuiceFX.PlayDropBounce(this.transform);

            if (myDockManager != null && mySlotIndex != -1)
            {
                myDockManager.NotifyDragSuccess(mySlotIndex);
            }

            if (gridManager != null)
            {
                gridManager.RunResolutionLoop();
            }

            Destroy(this); 
        }
        else
        {
            transform.position = originalPosition;
            if (myDockManager != null && mySlotIndex != -1)
            {
                myDockManager.NotifyDragFailed(mySlotIndex, this.gameObject);
            }
            JellyJuiceFX.PlayDropBounce(this.transform);
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mainCamera.transform.position.y - originalPosition.y; 
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }

    private bool TrySnapAndInjectData()
    {
        if (gridManager == null) return false;

        int gridLayerMask = LayerMask.GetMask("GridCell");
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 15f, gridLayerMask))
        {
            if (hit.collider.name.StartsWith("GridCell_"))
            {
                string[] parts = hit.collider.name.Split('_');
                if (parts.Length < 3) return false;

                int cellX = int.Parse(parts[1]);
                int cellY = int.Parse(parts[2]);
                Vector2Int targetCoord = new Vector2Int(cellX, cellY);

                if (!gridManager.TryGetCell(targetCoord, out GridCell targetCell)) return false;

                JellyBlockView[] childViews = GetComponentsInChildren<JellyBlockView>();
                if (childViews.Length == 0) return false;

                // 1. KIỂM TRA CHỖ TRỐNG
                foreach (var view in childViews)
                {
                    if (!targetCell.IsSlotFree(view.localSlot)) return false; 
                }

                // ===================================================================
                // FIX LỖI TRIỆT TIÊU SCALE: Ép cấu hình khay cha về mốc chuẩn 100% sạch sẽ
                // ===================================================================
                Vector3 targetCellPos = hit.collider.transform.position;
                
                // Đổi cha về GridManager nhưng KHÔNG giữ cấu hình thừa (false)
                transform.SetParent(gridManager.transform, false);
                
                // Ép cứng vị trí, trục xoay bằng 0 và kích thước chuẩn nguyên bản (1,1,1)
                transform.position = new Vector3(targetCellPos.x, originalPosition.y, targetCellPos.z);
                transform.rotation = Quaternion.identity;
                transform.localScale = Vector3.one; 

                // 2. ĐĂNG KÝ MA TRẬN LOGIC NGẦM
                foreach (var view in childViews)
                {
                    JellyBlock newLogicBlock = new JellyBlock(Random.Range(1000, 9999), view.blockColor, new List<Vector2Int> { view.localSlot });
                    newLogicBlock.VisualObjs.Add(view.gameObject);
                    targetCell.Blocks.Add(newLogicBlock);
                }

                // 3. KÍCH HOẠT CHUẨN HÓA KHUNG DIỆN TÍCH NGAY LẬP TỨC
                gridManager.NormalizeCellLayout(targetCell);

                // 4. DỌN DẸP RÁC VẬT LÝ
                BoxCollider parentCollider = GetComponent<BoxCollider>();
                if (parentCollider != null) Destroy(parentCollider);

                Transform bgLot = transform.Find("Img_Dock_Slot");
                if (bgLot != null) Destroy(bgLot.gameObject);

                return true;
            }
        }
        return false;
    }
}