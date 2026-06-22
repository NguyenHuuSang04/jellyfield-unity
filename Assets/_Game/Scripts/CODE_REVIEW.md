# 📋 ĐÁNH GIÁ CODE — DỰ ÁN JELLY FIELD

> Tài liệu này **chỉ đánh giá & đề xuất**, KHÔNG tự sửa code. Bạn đọc rồi tự chỉnh theo ý mình.
> Phạm vi: 20 file trong `Assets/_Game/Scripts`. Đánh giá theo 2 trục: **OOP** và **chuẩn phát triển game Unity**.

---

## 🎯 TÓM TẮT NHANH (TL;DR)

| Tiêu chí | Điểm | Nhận xét ngắn |
|---|---|---|
| Cấu trúc thư mục | 🟢 8/10 | Chia Core/Logic/Managers/View/UI/Level/Editor rất rõ ràng, chuyên nghiệp |
| Tách Logic / View | 🟡 5/10 | Có ý tưởng tách nhưng bị **rò rỉ**: model `JellyBlock` ôm `GameObject`, logic đọc ngược từ View |
| Đóng gói (Encapsulation) | 🔴 4/10 | Lạm dụng `public` cho gần như mọi field (đúng như bạn nghi ngờ) |
| Coupling (độ phụ thuộc) | 🔴 4/10 | Dùng `FindObjectByType` + parse tên string khắp nơi |
| Tính đúng đắn logic | 🟡 5/10 | Vài lỗi tiềm ẩn về merge, điều kiện thua, đếm goal |
| Code sạch (clean code) | 🟡 5/10 | Nhiều code chết (comment), magic number, thiếu namespace |
| Input | 🟡 6/10 | Dùng Input cũ (OnMouseDown) dù dự án bật New Input System |
| Điểm sáng | 🟢 | Interface generator, ScriptableObject, static resolver, event-driven |

**Kết luận:** Code **chạy được và có tư duy kiến trúc tốt** (hiếm với người mới), nhưng đang ở mức "prototype làm nhanh". Để đạt "chuẩn dev game" cần siết lại 3 thứ quan trọng nhất: **(1) đóng gói biến**, **(2) tách bạch Model–View**, **(3) bỏ `Find` + parse string**.

---

## ❓ TRẢ LỜI TRỰC TIẾP CÂU HỎI CỦA BẠN (public vs [SerializeField])

**Bạn nhớ ĐÚNG.** Quy ước chuẩn trong Unity:

- ❌ **Không nên** để `public` chỉ vì muốn kéo-thả trong Inspector.
- ✅ **Nên** dùng `[SerializeField] private` (hoặc `[SerializeField] private` + property `public` chỉ-đọc nếu nơi khác cần đọc).

**Vì sao?** `public` cho phép **bất kỳ script nào cũng sửa được** biến đó từ bên ngoài → phá vỡ tính đóng gói (encapsulation), khó kiểm soát bug. `[SerializeField] private` vẫn hiện trong Inspector để bạn gán, nhưng **chặn** code ngoài can thiệp.

```csharp
// ❌ Hiện tại (GridManager.cs, DockManager.cs, JellyBlockView.cs...)
public float cellSize = 1.4f;
public GameObject gridCellPrefab;

// ✅ Đề xuất
[SerializeField] private float cellSize = 1.4f;
[SerializeField] private GameObject gridCellPrefab;

// Nếu nơi khác CẦN ĐỌC cellSize (ví dụ DockManager, ClusterVisual đang đọc):
public float CellSize => cellSize;   // property chỉ-đọc, an toàn
```

**Một anti-pattern khác bạn đang mắc** trong `JellyBlockView.cs`:
```csharp
[HideInInspector] public Vector2Int localSlot;   // ❌ public mà lại ẩn đi
[HideInInspector] public BlockColor blockColor;
```
`[HideInInspector] public` là dấu hiệu của thiết kế chưa chuẩn: bạn muốn script khác truy cập nhưng không muốn lộ ở Inspector. Tốt hơn nên để `private` + hàm/property set có kiểm soát (giống `SetColorView` bạn đã làm — rất tốt, hãy làm tương tự cho `localSlot`).

**Danh sách file đang lạm dụng `public` field cần soát lại:**
- `GridManager.cs`: `cellSize`, `gridCellPrefab`, `jellyBlockPrefab`, `dropDelayBeforePop`, `cellGapPercent`, `currentLevelData`, `allLevels`
- `DockManager.cs`: toàn bộ field config (`clusterVisualPrefab`, `dockZPosition`, `slotSpacing`, ...)
- `DraggableGroup.cs`: `snapThreshold`
- `AudioManager.cs`: các `AudioClip`, `AudioSource`
- `JellyBlockView.cs`, `JellyJiggle.cs`, `ClusterVisual.cs`, `GoalItemUI.cs`, `GameUIManager.cs`, `MainMenuManager.cs`

> Lưu ý nhỏ: `[Header(...)]` đặt trên một **private field** (vd `DraggableGroup.cs` dòng 13–14: `[Header("Grid Snapping")] private GridManager gridManager;`) sẽ **không hiển thị gì cả** vì field private không được serialize. Header đó hiện đang vô tác dụng.

---

## 🏛️ A. KIẾN TRÚC & NGUYÊN TẮC OOP

### A1. 🔴 Model bị dính chặt vào View (vi phạm tách lớp)
`JellyBlock.cs` là class data thuần (tốt), NHƯNG lại chứa:
```csharp
public List<GameObject> VisualObjs = new List<GameObject>();
```
→ Lớp **dữ liệu logic** đang ôm tham chiếu **đối tượng hiển thị 3D**. Điều này làm:
- Không thể viết Unit Test cho logic mà không có scene/GameObject.
- Logic và hiển thị buộc phải sống chung, khó bảo trì.

**Nghiêm trọng hơn:** trong `GridManager.NormalizeCellLayout()` và `GridResolver`, code **đọc ngược dữ liệu logic từ View**:
```csharp
// GridManager.cs ~dòng 145, 179, 204, 217, 258
JellyBlockView v0 = b0.VisualObjs[0].GetComponent<JellyBlockView>();
bool isVerticalStack = (v0.localSlot.x == v1.localSlot.x);
```
Đúng nguyên tắc thì phải ngược lại: **Model là nguồn sự thật (source of truth)**, View chỉ vẽ lại theo Model. Hiện tại `JellyBlock` đã có `LocalSlots` rồi mà code lại đi hỏi `JellyBlockView.localSlot` → trùng lặp dữ liệu, dễ lệch pha.

**Đề xuất:** `JellyBlock` chỉ giữ dữ liệu (`Id`, `Color`, `LocalSlots`). Việc map sang GameObject nên do một lớp trung gian (vd `CellPresenter`/`GridView`) quản lý qua `Dictionary<JellyBlock, GameObject>`, thay vì nhét `GameObject` vào trong model.

### A2. 🔴 `GridManager` là "God Class" (vi phạm SRP)
File `GridManager.cs` (~420 dòng) đang ôm quá nhiều việc:
- Tạo lưới (`SetupLevelGrid`, `SetupDefaultRectangleGrid`)
- Tính layout + animation hiển thị (`NormalizeCellLayout`, `AnimateResize` — đây là việc của View!)
- Dọn bàn (`ClearActiveBoard`)
- Đi tìm Dock, bật/tắt object trong scene
- Điều phối resolver

**Đề xuất tách:**
- `GridBuilder` → chỉ dựng lưới từ `LevelData`
- `CellLayoutView` (hoặc gộp vào View) → lo phần `NormalizeCellLayout` + tween (vì đây 100% là hiển thị)
- `GridManager` → chỉ giữ `Dictionary<Vector2Int, GridCell>` và API truy vấn (`TryGetCell`, `ActiveCells`)

### A3. 🟡 Thiếu Namespace
Không file nào có `namespace`. Toàn bộ class nằm ở global namespace → nguy cơ trùng tên khi dự án lớn hoặc import asset ngoài (vd `GridManager`, `GameState` rất dễ đụng).
**Đề xuất:** bọc tất cả trong `namespace JellyField.Core`, `JellyField.Logic`, `JellyField.View`...

### A4. 🟢 Điểm tốt nên giữ
- `IClusterGenerator` (interface) → đúng nguyên tắc Dependency Inversion, dễ thêm chế độ sinh khác.
- `MergeResolver` / `MatchResolver` là `static class` thuần logic → tách tốt, có thể test.
- `GameManager` dùng `event` (`OnStateChanged`, `OnGoalUpdated`) → decoupling tốt; có `OnEnable/OnDisable` đăng ký/hủy đúng chuẩn.
- Dùng `ScriptableObject` cho `LevelData` → chuẩn Unity.
- `[RequireComponent]` → tốt.

---

## 🔗 B. ĐỘ PHỤ THUỘC (COUPLING)

### B1. 🔴 Lạm dụng `FindObjectByType` / `FindFirstObjectByType`
Xuất hiện ở: `DraggableGroup.Start`, `GameManager.SetupRuntimeGoals`, `GameUIManager` (nhiều chỗ), `GridManager.InitializeLevel` & `ClearActiveBoard`, và **nặng nhất là `ClusterVisual.BuildCluster`**:
```csharp
// ClusterVisual.cs ~dòng 19 — gọi MỖI LẦN dựng 1 cụm khối!
GridManager manager = Object.FindFirstObjectByType<GridManager>();
```
`Find...` rất chậm (quét toàn scene) và tạo phụ thuộc ngầm khó kiểm soát.
**Đề xuất:** Tiêm phụ thuộc (inject) qua tham số hoặc gán Inspector. Ví dụ `DockManager` đã truyền `gridManager` vào `ClusterVisual.BuildCluster(...)` thì truyền luôn `cellSize`, khỏi phải `Find`.

### B2. 🔴 Nhận diện ô lưới bằng cách parse TÊN string
```csharp
// DraggableGroup.cs ~dòng 129
if (hit.collider.name.StartsWith("GridCell_")) {
    string[] parts = hit.collider.name.Split('_');
    int cellX = int.Parse(parts[1]);
    int cellY = int.Parse(parts[2]);
}
```
Đây là cách rất dễ vỡ: chỉ cần đổi tên object, thêm hậu tố "(Clone)", hoặc đổi format → toàn bộ gãy. `int.Parse` cũng có thể ném exception.
**Đề xuất:** Gắn một component nhỏ lên prefab ô lưới, ví dụ `GridCellTag { public Vector2Int Coord; }`, rồi `hit.collider.GetComponent<GridCellTag>()` để lấy toạ độ. Nhanh, an toàn, không phụ thuộc tên.

### B3. 🟡 Tìm/bật/tắt object qua tên string trong `ClearActiveBoard`
```csharp
if (go.name.StartsWith("Static_Slot_BG")) ...
else if (go.name.StartsWith("Dock_Cluster") || go.name.Contains("DockSlotBG")) ...
```
Quét **toàn bộ** GameObject trong scene rồi so tên → vừa chậm vừa dễ vỡ. Nên để `DockManager` tự quản lý list object nó sinh ra (mà bạn đã có `activeSpawnedClusters`, `spawnedSlotBackgrounds`!) và cung cấp hàm `ClearAll()` thay vì đi tìm theo tên.

---

## 🐞 C. LỖI LOGIC TIỀM ẨN (quan trọng — nên kiểm tra kỹ)

### C1. 🔴 `MergeResolver` không xử lý đúng "connected-component"
Theo kế hoạch, merge phải gộp theo **cụm liền kề** (connected component). Nhưng code hiện tại gom **toàn bộ** khối cùng màu rồi validate adjacency theo kiểu "tất-cả-hoặc-không":
```csharp
// MergeResolver.cs ~dòng 40-65
combinedSlots.UnionWith(b.LocalSlots);   // gộp HẾT slot cùng màu
if (ValidateAdjacency(combinedSlots)) { ...gộp 1 khối... }
else { newBlocksList.AddRange(blocksOfColor); } // KHÔNG gộp gì cả
```
**Tình huống lỗi:** trong 1 ô có 3 khối cùng màu, 2 cái kề nhau + 1 cái nằm chéo. Đúng ra 2 cái kề phải gộp, cái chéo đứng riêng. Nhưng code union cả 3 → `ValidateAdjacency` thất bại (vì có cái chéo) → **giữ nguyên cả 3, không gộp gì**. Sai.
**Đề xuất:** Duyệt BFS/flood-fill để tách thành nhiều connected-component cùng màu, mỗi component gộp thành 1 khối riêng.

### C2. 🔴 Điều kiện THUA chưa khớp luật & chưa chính xác
```csharp
// GridResolver.cs ~dòng 131-139
bool isGridFull = true;
foreach (var cell in gridManager.ActiveCells)
    if (cell.Blocks.Count == 0) { isGridFull = false; break; }
```
Đang hiểu "đầy" = **mọi ô đều có ít nhất 1 khối**. Nhưng một ô có 1 khối vẫn còn 3 sub-slot trống → vẫn đặt thêm được. Vậy báo thua khi ô chưa thực sự đầy là **sai/ép thua sớm**. Ngược lại, kể cả khi mọi ô còn slot trống nhưng **cụm trên Dock không vừa bất kỳ ô nào** thì mới thực sự là thua — trường hợp này lại không được kiểm tra.
**Đề xuất:** Định nghĩa lại "thua" = *không còn nước đi hợp lệ cho các cụm đang ở Dock* (duyệt thử từng cụm Dock × từng ô × xem `IsSlotFree`). Hoặc tối thiểu kiểm tra theo **sub-slot trống** chứ không theo `Blocks.Count`.

### C3. 🟡 Đếm goal bằng `Count - 1` — nghi ngờ sai số
```csharp
// GridResolver.cs ~dòng 64
dynamicProgress = singleGroup.Count - 1;
...
GameManager.Instance.TrackPoppedBlocks(groupColor, dynamicProgress);
```
Một cụm match có N khối thì **cả N khối đều bị xoá**, nhưng chỉ trừ goal `N-1`. Nếu goal đếm theo "số khối màu đó đã clear" thì đây là **đếm thiếu**. Nếu chủ đích là "số lần match" thì nên đặt tên biến rõ hơn. Cần xác định lại ý đồ goal để tránh người chơi clear hết mà goal vẫn chưa về 0.

### C4. 🟡 Chỉ xử lý 1 match-group mỗi vòng lặp
```csharp
// GridResolver.cs ~dòng 55-56
matchGroups.Sort(...);
var singleGroup = matchGroups[0];   // chỉ lấy nhóm điểm cao nhất
```
Nếu 1 lượt thả tạo ra 2 cụm match độc lập, chỉ 1 cụm nổ ngay, cụm kia chờ vòng `while` sau. Vẫn chạy được nhờ `hasChanges=true`, nhưng combo/đếm có thể không như mong đợi. Cân nhắc xử lý tất cả group trong cùng một "đợt".

### C5. 🟡 `BagGridAwareGenerator` (túi bài thông minh cho Level 3) CHƯA được code
Kế hoạch có 2 generator, nhưng hiện chỉ có `FixedQueueGenerator`. Trong `GridManager.InitializeLevel`:
```csharp
// Ép TẤT CẢ level dùng hàng đợi cố định
IClusterGenerator levelGen = new FixedQueueGenerator(currentLevelData.PredefinedClusters);
```
→ Level 3 (Bag System + Grid-Aware) như đã chốt **chưa tồn tại**. `LevelData` cũng đã bỏ field `GenMode` (đang comment). Cần bổ sung nếu vẫn theo thiết kế cũ.

### C6. 🟡 `GameManager.AddGoalProgress(int)` là hàm rỗng (dead code)
```csharp
public void AddGoalProgress(int progress) {
    // Hàm phụ trợ để các script cũ gọi không bị crash compiler
}
```
`DebugOverlay` gọi hàm này để "giả lập thắng" nhưng nó **không làm gì** → nút debug vô tác dụng, gây hiểu nhầm. Nên xoá hoặc implement thật.

### C7. 🟡 ID khối tạo bằng `Random.Range` — có thể trùng
```csharp
new JellyBlock(Random.Range(1000, 9999), ...)
```
Random không đảm bảo duy nhất (dễ trùng). Hiện `Id` gần như không dùng để tra cứu nên chưa gây lỗi, nhưng nếu sau này dùng `Id` làm khóa → bug khó tìm. Nên dùng biến đếm tăng dần `static int _nextId++` hoặc `System.Guid`.

---

## 🎮 D. INPUT SYSTEM

### D1. 🟡 Dùng Input cũ trong khi dự án bật New Input System
`DraggableGroup.cs` dùng `OnMouseDown/OnMouseDrag/OnMouseUp` + `Input.mousePosition` (Legacy Input Manager). Dự án đang bật **cả hai** backend nên vẫn chạy (kể cả chạm trên Android được mô phỏng thành chuột), nhưng:
- Không đồng nhất với định hướng "New Input System" trong plan.
- `OnMouseX` phụ thuộc Physics Raycaster + thứ tự collider, kém linh hoạt cho multi-touch.

**Đề xuất:** Nếu không cần multi-touch ngay thì giữ cũng được cho prototype, nhưng nên ghi chú "nợ kỹ thuật". Khi nâng cấp, chuyển sang `UnityEngine.InputSystem` (`Pointer.current.position`, `EnhancedTouch`).

---

## ⚡ E. HIỆU NĂNG

- 🟡 `ClusterVisual.BuildCluster` gọi `FindFirstObjectByType<GridManager>()` mỗi lần dựng cụm (xem B1).
- 🟡 `ClearActiveBoard` và `InitializeLevel` gọi `FindObjectsByType<GameObject>(...)` quét **toàn scene** rồi so tên — rất nặng nếu scene nhiều object.
- 🟢 Các vòng lặp resolver O(số ô × số khối) là chấp nhận được với lưới nhỏ (tối đa 16 ô).
- 🟢 `JellyJiggle` có `OnDestroy` kill tween → tránh rò rỉ bộ nhớ, rất tốt.

---

## 🧹 F. CODE SẠCH (CLEAN CODE)

### F1. 🟡 Code chết (commented-out) nhiều
- `LevelData.cs`: ~59 dòng đầu là code cũ bị comment.
- `JellyJiggle.cs`: có **2 phiên bản đầy đủ** bị comment (dòng 1–260) trước bản đang dùng.
→ Nên xoá hẳn, dùng Git để lưu lịch sử thay vì giữ trong file (gây nhiễu, khó đọc).

### F2. 🟡 "Magic numbers" rải rác
Các số `0.27f`, `0.45f`, `0.3f`, `comboCount < 10`, `Random.Range(1000, 9999)`, `15f` (raycast distance)... nằm rải rác trong code.
**Đề xuất:** Gom thành `const`/`[SerializeField]` có tên rõ nghĩa (vd `MAX_COMBO_SAFETY = 10`, `popWaitTime`...).

### F3. 🟡 Lặp code (DRY)
`GridResolver.CheckBorderContactInternal` **trùng hoàn toàn** logic với `MatchResolver.CheckBorderContact`. Tương tự, logic tính layout cho 1/2/3/4 khối xuất hiện ở **cả** `GridManager.NormalizeCellLayout` **và** `ClusterVisual.BuildCluster` (gần như y hệt).
**Đề xuất:** Tách 1 hàm dùng chung (vd `CellGeometry.GetLayout(count, slot)` trả về pos+scale) để cả hai nơi gọi.

### F4. 🟢 Đặt tên biến tiếng Việt trong comment rất dễ hiểu, tinh thần giải thích tốt.

---

## 🛡️ G. AN TOÀN NULL (NULL-SAFETY)

Một số chỗ truy cập mảng/thành phần không kiểm tra, dễ ném exception:
```csharp
// GridManager.NormalizeCellLayout (count==2,3,4): giả định luôn có VisualObjs[0]
JellyBlockView v0 = b0.VisualObjs[0].GetComponent<JellyBlockView>();
```
Nếu `VisualObjs` rỗng → `IndexOutOfRange`; nếu thiếu component → `v0` null → NRE ở dòng sau. Nên thêm guard hoặc đảm bảo bất biến (invariant) "mỗi JellyBlock luôn có ≥1 visual".

---

## ✅ H. THỨ TỰ ƯU TIÊN SỬA (ROADMAP REFACTOR ĐỀ XUẤT)

| Ưu tiên | Việc | Lợi ích | Rủi ro khi sửa |
|---|---|---|---|
| 🔴 P1 | Đổi `public field` → `[SerializeField] private` (+ property nếu cần đọc) | Đóng gói, đúng chuẩn | Thấp (chỉ cần gán lại vài chỗ đọc) |
| 🔴 P1 | Thay parse-tên-string bằng `GridCellTag` component (B2) | Hết lỗi vỡ ngầm | Thấp |
| 🔴 P2 | Sửa điều kiện THUA theo sub-slot/nước đi (C2) | Đúng luật chơi | Trung bình |
| 🔴 P2 | Sửa `MergeResolver` thành connected-component (C1) | Đúng cơ chế gộp | Trung bình |
| 🟡 P3 | Bỏ `FindObjectByType`, chuyển sang inject (B1, B3) | Hiệu năng + decoupling | Trung bình |
| 🟡 P3 | Tách phần layout/animation khỏi `GridManager` (A2) | Dễ bảo trì | Cao (động vào nhiều) |
| 🟡 P4 | Gỡ code chết, gom magic number, thêm namespace (F1, F2, A3) | Sạch, dễ đọc | Thấp |
| 🟡 P4 | Bỏ `VisualObjs` khỏi model (A1) | Test được logic | Cao |
| 🟢 P5 | Xem lại `dynamicProgress - 1` (C3), generator L3 (C5) | Đúng goal/thiết kế | Tùy thiết kế |

---

## 🌟 KẾT LUẬN

Bạn có **tư duy kiến trúc tốt hơn mức "người mới"**: biết tách thư mục theo vai trò, dùng interface, ScriptableObject, static resolver, event. Đây là nền móng vững.

Những điểm cần siết để lên "chuẩn dev game" tập trung vào **kỷ luật OOP**: (1) đừng `public` bừa — hãy `[SerializeField] private`; (2) giữ Model là nguồn sự thật, đừng để logic đọc ngược từ View; (3) loại bỏ phụ thuộc ngầm (`Find` + parse tên). Ba điều này nếu làm tốt sẽ nâng chất lượng code lên rõ rệt.

Phần lỗi logic (merge connected-component, điều kiện thua, đếm goal) nên ưu tiên kiểm tra vì ảnh hưởng trực tiếp tới trải nghiệm chơi.

> Khi nào bạn muốn mình **thực sự sửa** bất kỳ mục nào ở trên, chỉ cần nói rõ mục đó (vd "sửa giúp mình P1 và C2"), mình sẽ chỉnh từng phần một cách an toàn nhé. 💪
