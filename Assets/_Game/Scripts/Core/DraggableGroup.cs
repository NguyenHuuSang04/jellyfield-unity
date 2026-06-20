// using UnityEngine;

// public class DraggableGroup : MonoBehaviour
// {
//     private Camera mainCamera;
//     private Vector3 offset;
//     private bool isDragging = false;
//     private Vector3 originalPosition;
//     private float dragHeightOffset = 0.5f; // Nâng nhẹ cụm khối lên khi kéo để tạo cảm giác 3D

//     void Start()
//     {
//         mainCamera = Camera.main;
//         originalPosition = transform.position;
//     }

//     void OnMouseDown()
//     {
//         // Khi người chơi chạm/click vào cụm khối 3D (Yêu cầu Object phải có Collider)
//         isDragging = true;

//         Vector3 mouseWorldPos = GetMouseWorldPosition();
//         offset = transform.position - mouseWorldPos;
//     }

//     void OnMouseDrag()
//     {
//         if (!isDragging) return;

//         Vector3 mouseWorldPos = GetMouseWorldPosition();
//         Vector3 targetPos = mouseWorldPos + offset;

//         // Giữ cao độ Y cố định (cộng thêm một khoảng nâng nhẹ để block nổi lên trên lưới)
//         targetPos.y = originalPosition.y + dragHeightOffset;

//         transform.position = targetPos;
//     }

//     void OnMouseUp()
//     {
//         isDragging = false;

//         // Tạm thời trả về vị trí cũ ở khay Dock. 
//         // Ngày mai tụi mình sẽ viết code kiểm tra tọa độ ô lưới để Snap vào sau.
//         transform.position = originalPosition;

//         // CHÈN LỆNH TEST: Khi búng về vị trí cũ, chạy hiệu ứng thạch nảy dập dình!
//         JellyJuiceFX.PlayDropBounce(this.transform);
//     }

//     // Hàm toán học chuyển đổi tọa độ màn hình (2D) sang thế giới game (3D mặt phẳng XZ)
//     private Vector3 GetMouseWorldPosition()
//     {
//         Vector3 mousePoint = Input.mousePosition;
//         // Tính khoảng cách từ Camera (Y=10) xuống cao độ mặc định của khay dưới (Y=0.5)
//         mousePoint.z = mainCamera.transform.position.y - originalPosition.y;
//         return mainCamera.ScreenToWorldPoint(mousePoint);
//     }
// }


using UnityEngine;

public class DraggableGroup : MonoBehaviour
{
    private Camera mainCamera;
    private Vector3 offset;
    private bool isDragging = false;
    private Vector3 originalPosition;
    private float dragHeightOffset = 0.5f;

    [Header("Grid Snapping")]
    private GridManager gridManager;
    public float snapThreshold = 1.0f; // Khoảng cách tối đa để nhận diện hút vào ô lưới

    void Start()
    {
        mainCamera = Camera.main;
        originalPosition = transform.position;
        
        // Tự động tìm GridManager đang có trong Scene
        gridManager = Object.FindFirstObjectByType<GridManager>();
    }

    void OnMouseDown()
    {
        isDragging = true;
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        offset = transform.position - mouseWorldPos;
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

        // XỬ LÝ SNAP VÀO LƯỚI KHI THẢ CHUỘT
        if (TrySnapToGrid())
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(AudioManager.Instance.dropSound);
            // Nếu đặt khối thành công:
            Debug.Log($"[Draggable]: Đặt cụm khối vào lưới thành công!");
            
            // Chạy hiệu ứng thạch rơi bộp xuống dập dình
            JellyJuiceFX.PlayDropBounce(this.transform);

            // Kích hoạt vòng lặp giải quyết Combo (Gộp & Nổ) trên GridManager
            if (gridManager != null)
            {
                gridManager.RunResolutionLoop();
            }

            // (Trong bản game thực tế, cụm khối này sẽ bị khóa hoặc phá hủy để sinh cụm mới ở Dock)
        }
        else
        {
            // Nếu thả hụt ra ngoài hoặc ô lưới không hợp lệ -> Búng cụm khối về lại khay Dock
            transform.position = originalPosition;
            JellyJuiceFX.PlayDropBounce(this.transform);
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mainCamera.transform.position.y - originalPosition.y; 
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }

    // Thuật toán Raycast dò tìm ô lưới lớn dưới vị trí ngón tay/chuột
    private bool TrySnapToGrid()
    {
        if (gridManager == null) return false;

        // Bắn một tia Ray từ vị trí hiện tại của cụm khối thẳng xuống mặt phẳng dưới (trục -Y)
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        // Tạo một LayerMask "Grid" nếu cần, hoặc tạm thời raycast trúng Collider của ô lưới
        if (Physics.Raycast(ray, out hit, 15f))
        {
            // Kiểm tra xem Object chạm trúng có phải là một GridCell hiển thị không
            if (hit.collider.name.StartsWith("GridCell_"))
            {
                // Trích xuất tọa độ hiển thị của ô lưới vừa chạm trúng
                Vector3 targetCellPos = hit.collider.transform.position;
                
                // Giữ nguyên cao độ Y của cụm khối để nằm đè lên mặt phẳng lưới
                targetCellPos.y = originalPosition.y; 

                // Hút cụm khối dính chặt vào tâm của ô lưới lớn (Snap)
                transform.position = targetCellPos;
                return true;
            }
        }

        return false;
    }
}