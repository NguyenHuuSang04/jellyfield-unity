using UnityEngine;
using System.Collections.Generic;
using JellyField.View;
using JellyField.Managers;

namespace JellyField.Core
{
    public class DraggableGroup : MonoBehaviour
    {
        private Camera mainCamera;
        private Vector3 offset;
        private bool isDragging = false;
        private Vector3 lastFramePosition;
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
            gridManager = GridManager.Instance;
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
            lastFramePosition = transform.position;
        }

        void OnMouseDrag()
        {
            if (!isDragging) return;

            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector3 targetPos = mouseWorldPos + offset;
            targetPos.y = originalPosition.y + dragHeightOffset;

            transform.position = targetPos;

            // Tính toán tốc độ vuốt và hướng vuốt chuột/tay của người chơi
            Vector3 dragVelocity = (transform.position - lastFramePosition) / Time.deltaTime;
            Vector3 dragDir = dragVelocity.normalized;

            // Quét qua các khối thạch con đang bị kéo và ra lệnh jiggle uốn lượn theo hướng vuốt
            JiggleJiggleAndDrag(dragVelocity, dragDir);

            lastFramePosition = transform.position; // Cập nhật mốc vị trí frame
        }

        private void JiggleJiggleAndDrag(Vector3 dragVelocity, Vector3 dragDir)
        {
            JellyJiggle[] jiggles = GetComponentsInChildren<JellyJiggle>();
            foreach (var jiggle in jiggles)
            {
                if (jiggle != null) jiggle.UpdateJiggleOnDrag(dragVelocity.magnitude, dragDir);
            }
        }

        void OnMouseUp()
        {
            isDragging = false;

            // XỬ LÝ SNAP VÀ INJECT DỮ LIỆU VÀO LƯỚI NGẦM
            if (TrySnapAndInjectData())
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.DropSound);

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
            else // TRƯỜNG HỢP KÉO RỚT RA NGOÀI LƯỚI (FAILED)
            {
                transform.position = originalPosition;
                if (myDockManager != null && mySlotIndex != -1)
                {
                    myDockManager.NotifyDragFailed(mySlotIndex, this.gameObject);
                }
                JellyJuiceFX.PlayDropBounce(this.transform);

                // Kích hoạt hiệu ứng đàn hồi bẹp dí khi bay về khay Dock
                // (Giúp trả viên thạch từ trạng thái "đang bị kéo dãn" về phom vuông vức ban đầu)
                JellyJiggle[] jigglesOnFail = GetComponentsInChildren<JellyJiggle>();
                foreach (var jiggle in jigglesOnFail)
                {
                    if (jiggle != null) jiggle.PlayDropJiggle();
                }
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
                GridCellTag cellTag = hit.collider.GetComponent<GridCellTag>();
                if (cellTag != null)
                {
                    Vector2Int targetCoord = cellTag.Coord;

                    if (!gridManager.TryGetCell(targetCoord, out GridCell targetCell)) return false;

                    JellyBlockView[] childViews = GetComponentsInChildren<JellyBlockView>();
                    if (childViews.Length == 0) return false;

                    // 1. KIỂM TRA CHỖ TRỐNG
                    foreach (var view in childViews)
                    {
                        if (!targetCell.IsSlotFree(view.LocalSlot)) return false;
                    }

                    // Ép cấu hình khay cha về mốc chuẩn 100% sạch sẽ
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
                        JellyBlock newLogicBlock = new JellyBlock(JellyBlock.GenerateUniqueId(), view.BlockColor, new List<Vector2Int> { view.LocalSlot });
                        gridManager.RegisterVisuals(newLogicBlock.Id, new List<GameObject> { view.gameObject });
                        targetCell.Blocks.Add(newLogicBlock);
                    }

                    // 3. KÍCH HOẠT CHUẨN HÓA KHUNG DIỆN TÍCH NGAY LẬP TỨC
                    gridManager.NormalizeCellLayout(targetCell);

                    // 4. DỌN DẸP RÁC VẬT LÝ
                    BoxCollider parentCollider = GetComponent<BoxCollider>();
                    if (parentCollider != null) Destroy(parentCollider);

                    Transform bgLot = transform.Find("Img_Dock_Slot");
                    if (bgLot != null) Destroy(bgLot.gameObject);

                    //  Kích hoạt hiệu ứng nẩy bẹp dí rồi đàn hồi khi vừa đặt thạch chạm lưới thành công!
                    JellyJiggle[] jigglesOnDrop = GetComponentsInChildren<JellyJiggle>();
                    foreach (var jiggle in jigglesOnDrop)
                    {
                        if (jiggle != null) jiggle.PlayDropJiggle();
                    }

                    return true;
                }
            }
            return false;
        }
    }
}