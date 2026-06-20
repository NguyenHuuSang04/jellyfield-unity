# JELLY FIELD — Sprint Roadmap 4 Ngày (Prototype từ con số 0)

> ⚠️ **Lưu ý về giả định:** Bạn chưa kịp trả lời 3 câu hỏi làm rõ (timeout). Mình đã chọn các giả định hợp lý nhất (đánh dấu **⚠️ GIẢ ĐỊNH**) dựa trên mô tả + ảnh mockup. Vui lòng kiểm tra và phản hồi ở bước review để mình điều chỉnh trước khi code.

---

# Project Overview

- **Game Title:** JELLY FIELD
- **High-Level Concept:** Game puzzle casual 3D dạng lưới (grid-based). Người chơi kéo-thả các cụm khối thạch 3D nhiều màu từ khay Dock dưới đáy lên các ô trống trên một lưới tùy biến theo level. Khối cùng màu sẽ gộp/phình to và match theo chuỗi để hoàn thành mục tiêu (Goals) từng màu.
- **Players:** Single player (offline).
- **Inspiration / Reference Games:** Block Blast / Woodoku (cơ chế kéo cụm từ Dock) + Jelly Merge / Match cơ chế gộp màu.
- **Tone / Art Direction:** Casual, dễ thương, thạch jelly 3D bóng bẩy mọng nước (juicy), nền tối tương phản cao để khối màu nổi bật.
- **Target Platform:** Android (chính), iOS (mở rộng sau).
- **Screen Orientation / Resolution:** Portrait 1080x1920.
- **Render Pipeline:** URP (đã có sẵn `Mobile_RPAsset` / `PC_RPAsset` trong `Assets/Settings`).

## Phạm vi Prototype — Định nghĩa "Done"
Mục tiêu là **bản gameplay chơi được (playable core)**, KHÔNG kèm UI:
- 3 level theo thông số: L1 chữ thập, L2 chữ nhật 4x5, L3 kim cương 16 ô.
- Lưới logic 2x2 lồng nhau hoạt động đúng.
- Kéo-thả cụm khối từ Dock bằng Raycast (touch/mouse).
- Logic gộp trong ô (intra-cell merge + resize) và match giữa các ô (inter-cell clear) chạy theo chuỗi.
- Hệ thống Dock: L1 (1 slot) / L2 (2 slot) hardcode; L3 (2 slot) dùng Bag System + Grid-Aware.
- Điều kiện Win (đủ Goals từng màu) / Lose (lưới không còn ô trống) hoạt động — phát **event** để UI lắng nghe sau.
- Hiệu ứng "juicy" cơ bản (tween scale khi gộp/clear) — không cần shader thạch hoàn chỉnh.

**Ngoài phạm vi (Out of scope) — bạn cung cấp/đặt làm sau:**
- **Toàn bộ UI**: Main Menu, HUD (Level label, nút Home/Retry), Goals Panel, popup Win/Lose. Gameplay chỉ phát event + expose data; chưa vẽ UI.
- Meta progression, lưu tiến trình, âm thanh hoàn chỉnh, shader thạch trong suốt nâng cao, đa ngôn ngữ, monetization.

> **Cầu nối với UI (để ghép sau không phải refactor):** `GameManager` sẽ phát các C# event/`UnityEvent`: `OnGoalsChanged(GoalState)`, `OnLevelWon(int level)`, `OnLevelLost()`, `OnLevelLoaded(LevelData)`. UI sau này chỉ subscribe các event này.

---

# Game Mechanics

## Core Gameplay Loop
1. Người chơi nhìn lưới level hiện tại + Goals (vd: Tím x4, Xanh lá x4).
2. Dưới đáy là Dock chứa (các) cụm khối thạch.
3. Người chơi **kéo nguyên cụm** từ Dock lên một **ô lưới lớn còn trống** (hoặc còn slot con trống).
4. Khi thả vào ô → chạy **vòng giải quyết (resolution loop)**:
   - **Phase A — Gộp trong ô (Intra-cell Merge):** Trong sub-grid 2x2 của ô, các khối con cùng màu nằm kề nhau (ngang/dọc, không tính chéo) gộp lại và phình to để lấp đầy diện tích (1x1+1x1 → 1x2/2x1; 4 khối → 2x2 chiếm trọn ô).
   - **Phase B — Match giữa ô (Inter-cell Match/Clear):** Quét biên giữa các ô lớn kề nhau; nếu khối cùng màu áp sát nhau qua biên thỏa điều kiện match → clear (xóa) và cộng vào Goal.
   - **Chain:** Sau khi clear/resize tạo ra adjacency mới, lặp lại Phase A→B cho đến khi ổn định.
5. Cập nhật Goals. Nếu đủ chỉ tiêu mỗi màu → **Win**. Nếu lưới không còn ô trống nào → **Lose**.
6. Lặp lại ở level tiếp theo.

### ✅ Luật #1 — Match giữa ô (đã chốt)
**"Bất kỳ khối cùng màu áp sát qua biên ô đều match".** Khi 2 khối cùng màu ở 2 ô kề nhau có cạnh tiếp xúc **trùng hàng/cột sub-slot** → cả 2 (và toàn bộ chuỗi liên đới qua nhiều ô) bị clear và cộng vào Goal.

### ✅ Luật #2 — Win / Lose (đã chốt)
- **Win:** Clear đủ số lượng cho **từng màu** ghi trong `Goals` (vd: Tím x4, Xanh lá x4). Khi mọi mục Goal về 0 → Win.
- **Lose:** Khi **lưới không còn ô trống** (không còn slot con nào trống ở mọi ô active) mà chưa đạt Goal → Lose.

### ✅ Luật #3 — Dock & Sinh khối (đã chốt)
| Level | Số slot Dock | Cách sinh cụm |
|-------|--------------|----------------|
| L1 (chữ thập) | **1 slot** | Hardcode cố định trong `LevelData.FixedQueue` |
| L2 (4x5) | **2 slot** | Hardcode cố định trong `LevelData.FixedQueue` |
| L3 (kim cương 16 ô) | **2 slot** | **Bag System + Grid-Aware** (xem mục "Hệ thống sinh khối Dock") |

Chi tiết thuật toán L3 (Bag + Grid-Aware) được mô tả đầy đủ ở mục **"Hệ thống sinh khối Dock (TRỌNG TÂM — Luật #3)"** bên dưới.

## Controls and Input Methods
- **New Input System** (đã bật trong project, có sẵn `InputSystem_Actions.inputactions`).
- **Touch/Mouse drag:** Dùng `<Pointer>/position` + `<Pointer>/press`.
- **Raycast pick-up:** Khi nhấn xuống → Physics raycast từ Camera (orthographic) qua vị trí con trỏ, layer `Dock`, để chọn cụm khối.
- **Drag:** Cụm bám theo con trỏ trên mặt phẳng XZ (project pointer ray xuống mặt phẳng Y=0 chơi), có offset nâng nhẹ + bóng "ghost" ô đích.
- **Drop:** Khi nhả → raycast layer `Grid` tìm ô đích; validate (ô có đủ slot trống & cụm vừa chỗ) → snap vào ô, nếu không hợp lệ → tween trả cụm về Dock.

---

# UI
> ⛔ **Ngoài phạm vi prototype này** — bạn sẽ cung cấp UI sau. Gameplay chỉ render **3D world** (lưới + khối + Dock đều là 3D mesh, render bởi Main Camera Orthographic xoay X=90 top-down) và **phát event** để UI ghép vào sau:
> - `OnLevelLoaded(LevelData level)` — khi level nạp xong (để UI vẽ Goals ban đầu, Level label).
> - `OnGoalsChanged(IReadOnlyList<GoalEntry> remaining)` — mỗi khi clear cập nhật goal.
> - `OnLevelWon(int levelIndex)` / `OnLevelLost()` — để UI bật popup.
>
> Trong prototype, các thông tin này sẽ được **log ra Console + Debug overlay tạm (OnGUI hoặc TextMeshPro world-space)** để kiểm thử, không phải UI chính thức.

---

# Key Asset & Context

## Cấu trúc thư mục đề xuất
```
Assets/
  _Game/
    Scripts/
      Core/          (GridManager, GridCell, JellyBlock, SubSlot, BlockColor, BlockShape)
      Logic/         (MergeResolver, MatchResolver, ResolutionLoop)
      Input/         (DragController, InputReader)
      Level/         (LevelData (SO), LevelLoader, ClusterData)
      Gameplay/      (GameManager, GoalTracker, DockManager, IClusterGenerator, FixedQueueGenerator, BagGridAwareGenerator)
      View/          (JellyBlockView, GridCellView, ClusterView, JuiceFX, DebugOverlay)
    Prefabs/         (JellyBlock, GridCell, Cluster)
    ScriptableObjects/Levels/ (Level_01, Level_02, Level_03)
    Materials/       (Jelly_Purple, Jelly_Green, Jelly_Cyan ... URP Lit Smoothness cao)
    Scenes/          (Game.unity)  hoặc tái dùng SampleScene.unity
```

## Cấu trúc dữ liệu cốt lõi (TRỌNG TÂM)

### `BlockColor` (enum)
```csharp
public enum BlockColor { None, Purple, Green, Cyan, Yellow, Red, Blue }
```

### Sub-slot trong ô (toạ độ local 2x2)
Mỗi `GridCell` có sub-grid 2x2. Local slot dùng `Vector2Int` với x,y ∈ {0,1}:
```
(0,1)(1,1)   <- hàng trên (TL, TR)
(0,0)(1,0)   <- hàng dưới (BL, BR)
```

### `JellyBlock` (data thuần, không MonoBehaviour)
```csharp
public class JellyBlock {
    public BlockColor Color;
    public HashSet<Vector2Int> LocalSlots; // các sub-slot mà khối chiếm trong ô (1..4 ô)
    public int Id;                          // để map sang view
    // Footprint: 1 slot=1x1, 2 slot kề ngang/dọc=1x2/2x1, 4 slot=2x2
}
```

### `GridCell` (data thuần)
```csharp
public class GridCell {
    public Vector2Int Coord;                // vị trí ô trong lưới toàn cục
    public List<JellyBlock> Blocks;         // 1..4 khối; ràng buộc: 2 khối chưa gộp phải KHÁC màu
    public bool IsActive;                    // ô có tồn tại trong shape level không
    public bool IsSlotFree(Vector2Int local);
    public bool IsFull();
}
```

### `GridManager` (MonoBehaviour, sparse để hỗ trợ shape bất kỳ)
```csharp
public class GridManager : MonoBehaviour {
    Dictionary<Vector2Int, GridCell> _cells;  // sparse -> hỗ trợ chữ thập / kim cương
    public bool TryGetCell(Vector2Int c, out GridCell cell);
    public IEnumerable<GridCell> ActiveCells { get; }
    public Vector3 CellToWorld(Vector2Int c);          // map logic -> world XZ
    public bool WorldToCell(Vector3 world, out Vector2Int c);
}
```
> **Lý do dùng `Dictionary` thay vì mảng 2D:** Shape lưới (chữ thập, kim cương) thưa và bất đối xứng. Sparse map cho phép định nghĩa ô tồn tại tuỳ ý qua `LevelData`, tránh ô "ma" và đơn giản hoá việc duyệt láng giềng.

### `LevelData` (ScriptableObject)
```csharp
public enum DockGenMode { FixedQueue, BagGridAware }

[CreateAssetMenu(menuName="JellyField/Level")]
public class LevelData : ScriptableObject {
    public int LevelIndex;
    public List<Vector2Int> ActiveCells;          // shape lưới
    public List<PrefilledBlock> PrefilledBlocks;  // khối có sẵn trên lưới lúc start (như mockup L1)
    public List<GoalEntry> Goals;                 // (BlockColor, count)

    [Header("Dock")]
    public int DockSlotCount = 1;                 // L1=1, L2=2, L3=2
    public DockGenMode GenMode = DockGenMode.FixedQueue;
    public List<ClusterData> FixedQueue;          // dùng khi GenMode=FixedQueue (L1, L2)
    public List<ClusterData> Bag;                 // "túi" 10 cụm dùng khi GenMode=BagGridAware (L3)
}

[System.Serializable]
public struct PrefilledBlock { public Vector2Int Cell; public Vector2Int LocalSlot; public BlockColor Color; }
[System.Serializable]
public struct ClusterData { public List<BlockCell> Blocks; }   // 1 cụm = nhiều khối con tại local slot
[System.Serializable]
public struct BlockCell  { public Vector2Int LocalSlot; public BlockColor Color; }
[System.Serializable]
public struct GoalEntry  { public BlockColor Color; public int Count; }
```
> **Lưu ý serialize:** Unity không serialize tuple `(Vector2Int, BlockColor)`, nên tách thành struct `BlockCell`/`PrefilledBlock` có `[System.Serializable]` để chỉnh được trong Inspector.

## Thuật toán (TRỌNG TÂM)

### Phase A — Intra-cell Merge
```
Cho mỗi GridCell thay đổi:
  Nhóm các JellyBlock theo Color.
  Với mỗi color có >=2 khối:
    Dựng đồ thị adjacency 4-hướng giữa các LocalSlot của color đó.
    Tìm các connected-component.
    Mỗi component => hợp nhất thành 1 JellyBlock chiếm union các slot.
  (2 khối chéo nhau, không kề => KHÔNG gộp, vẫn là 2 khối riêng)
```

### Phase B — Inter-cell Match/Clear
```
Dựng đồ thị "segment" qua biên ô:
  Với mỗi cặp ô kề nhau (A,B) theo hướng H:
    Lấy các sub-slot của A áp biên về phía B, và của B áp biên về phía A.
    Nếu tại cùng hàng/cột sub-slot, A và B có khối CÙNG MÀU tiếp xúc:
      Nối 2 khối đó vào cùng match-group.
  Mọi match-group (>=2 khối) => đánh dấu clear.
Xoá các khối clear, cộng count theo màu vào GoalTracker.
```

### Resolution Loop (chain)
```
loop:
  changed = RunPhaseA() | RunPhaseB()
  if !changed: break
Cập nhật view (tween) sau mỗi vòng để tạo cảm giác chain "juicy".
```

> **Quyết định thiết kế:** Tách **Logic (data thuần, test được bằng unit test)** khỏi **View (MonoBehaviour, tween, mesh)**. Logic không phụ thuộc Unity scene → có thể viết EditMode test cho merge/match, giảm rủi ro bug ở phần phức tạp nhất.

## Hệ thống sinh khối Dock (TRỌNG TÂM — Luật #3)

Dùng **interface chung** để `DockManager` không cần biết level đang dùng cách sinh nào → dễ mở rộng:
```csharp
public interface IClusterGenerator {
    bool TryGetNext(GridManager grid, out ClusterData cluster); // false = hết khối
}
```

### A. `FixedQueueGenerator` — cho L1 & L2 (hardcode)
```
Khởi tạo: copy LevelData.FixedQueue -> Queue<ClusterData>.
TryGetNext: nếu queue rỗng -> false; ngược lại Dequeue và trả ra.
```
- Tính chất: **deterministic 100%** → puzzle L1/L2 luôn giải được đúng như thiết kế, dễ test/lặp.
- `DockSlotCount` slot luôn được giữ đầy bằng cách gọi `TryGetNext` cho tới khi đủ slot hoặc hết queue.

### B. `BagGridAwareGenerator` — cho L3 (Bag System + Grid-Aware)
Kết hợp đúng 2 quy tắc bạn mô tả:
```
State: List<ClusterData> _bag;   // túi hiện tại (rút ra dần)
Khởi tạo / Refill: _bag = new copy của LevelData.Bag (vd 10 cụm).

TryGetNext(grid):
  // Bước 3 - tự nạp đầy khi rỗng
  if (_bag.Count == 0) RefillBag();

  // Bước 1 - Grid-Aware: đếm số lượng từng màu đang nằm trên lưới
  colorCount = grid quét tất cả JellyBlock -> Dictionary<BlockColor,int>
  desiredColors = các màu có count > 0, sắp xếp giảm dần (màu chiếm đa số trước)

  // Bước 2 - Lọc túi theo màu cần
  foreach color in desiredColors:
     candidate = cụm đầu tiên trong _bag chứa color này
     if (candidate tồn tại) { lấy ra khỏi _bag; return candidate; }

  // Fallback: lưới trống hoặc túi không có cụm phù hợp -> bốc ngẫu nhiên
  pick = _bag[Random.Range(0, _bag.Count)];
  remove pick from _bag; return pick;
```
- **Quy tắc 1 (Grid-Aware):** ưu tiên cụm chứa màu đang nhiều trên lưới → tăng cơ hội merge & giải phóng ô.
- **Quy tắc 2 (Bag):** chỉ bốc trong túi đã thiết kế (đảm bảo về tổng thể có thể giải được), tạo cảm giác ngẫu nhiên có kiểm soát.
- **Bước 3 (Refill):** túi rỗng → nạp lại bản copy mới của `LevelData.Bag`.
- Để test ổn định: cho phép inject `System.Random` seed (constructor) → EditMode test deterministic được.

> **Quyết định thiết kế:** Đặt generator sau interface giúp `DockManager` chỉ gọi `TryGetNext` mỗi khi một slot trống cần nạp; logic generator là **data thuần (không MonoBehaviour)** nên test được không cần scene.

---

# Implementation Steps

> Định dạng mỗi bước: **Mô tả** · **Vai trò** · **Phụ thuộc** · **Song song được?**

## GIAI ĐOẠN 1 — Nền tảng dự án + Cấu trúc dữ liệu Grid
**Bước 1.1 — Thiết lập project & scene (gameplay-only, KHÔNG Canvas UI)**
- Tạo cấu trúc thư mục `_Game/...`. Tạo `Game.unity` (hoặc dùng SampleScene). Cấu hình Main Camera: Orthographic, rotation X=90 (top-down), điều chỉnh `orthographicSize` cho khung dọc 1080x1920. Gán `Mobile_RPAsset`. Set Player Settings Portrait. Thêm 1 Directional Light cho khối thạch bắt sáng.
- **Vai trò:** developer · **Phụ thuộc:** None · **Song song:** Có (cùng 1.2)

**Bước 1.2 — Lớp dữ liệu Core (data thuần)**
- Tạo `BlockColor`, `JellyBlock`, `GridCell`, `GridManager` (sparse dict), `LevelData` (SO) + struct phụ. Chưa cần view.
- File: `Core/BlockColor.cs`, `Core/JellyBlock.cs`, `Core/GridCell.cs`, `Core/GridManager.cs`, `Level/LevelData.cs`.
- **Vai trò:** developer · **Phụ thuộc:** None · **Song song:** Có (cùng 1.1)

**Bước 1.3 — LevelLoader + render lưới cơ bản**
- `LevelLoader` đọc `LevelData` → tạo `GridCell` trong `GridManager` + spawn prefab `GridCellView` đúng vị trí world (CellToWorld). Tạo `LevelData` asset cho L1 (chữ thập) + prefilled blocks như mockup.
- File: `Level/LevelLoader.cs`, `View/GridCellView.cs`, SO `Level_01`.
- **Vai trò:** developer · **Phụ thuộc:** 1.1, 1.2 · **Song song:** Không

## GIAI ĐOẠN 2 — Khối thạch View + Dock + Raycast Drag & Drop
**Bước 2.1 — JellyBlock View + Materials**
- Prefab `JellyBlock` (mesh bo góc / placeholder rounded cube) + `JellyBlockView` map từ `JellyBlock` data (màu, footprint → scale/position trong ô). Materials URP Lit Smoothness cao cho từng màu.
- File: `View/JellyBlockView.cs`, Prefabs + Materials.
- **Vai trò:** developer · **Phụ thuộc:** Giai đoạn 1 · **Song song:** Có (cùng 2.2)

**Bước 2.2 — Dock + Cluster + Generator (Luật #3)**
- Định nghĩa `IClusterGenerator`; cài `FixedQueueGenerator` (L1/L2) và `BagGridAwareGenerator` (L3). `DockManager` đọc `LevelData.GenMode` + `DockSlotCount`, giữ các slot luôn đầy bằng `TryGetNext`. `ClusterView` hiển thị cụm world-space dưới đáy, có Collider (layer `Dock`).
- File: `Gameplay/IClusterGenerator.cs`, `Gameplay/FixedQueueGenerator.cs`, `Gameplay/BagGridAwareGenerator.cs`, `Gameplay/DockManager.cs`, `View/ClusterView.cs`, Prefab `Cluster`.
- **Vai trò:** developer · **Phụ thuộc:** 2.1, Giai đoạn 1 (data) · **Song song:** Một phần (generator là data thuần, làm song song với 2.1)

**Bước 2.3 — Raycast Drag Controller (New Input System)**
- `InputReader` (bind pointer position/press). `DragController`: pick cụm (raycast layer Dock) → drag theo mặt phẳng XZ → drop raycast layer Grid → validate đặt vào ô (đủ slot trống) → đặt khối vào `GridCell` data + spawn `JellyBlockView`; không hợp lệ thì tween về Dock. Hiện "ghost" ô đích. Sau khi đặt thành công, DockManager nạp lại slot trống.
- File: `Input/InputReader.cs`, `Input/DragController.cs`.
- **Vai trò:** developer · **Phụ thuộc:** 2.1, 2.2 · **Song song:** Không

## GIAI ĐOẠN 3 — Logic Gộp & Match (TRỌNG TÂM)
**Bước 3.1 — Phase A: Intra-cell Merge**
- `MergeResolver`: gom theo màu, connected-component trên sub-slot, hợp nhất khối + cập nhật footprint. Trả về danh sách thay đổi để view tween resize.
- File: `Logic/MergeResolver.cs`.
- **Vai trò:** developer · **Phụ thuộc:** Ngày 1 (data) · **Song song:** Có (cùng 3.2 - thuần logic)

**Bước 3.2 — Phase B: Inter-cell Match/Clear**
- `MatchResolver`: duyệt cặp ô kề, dựng match-group qua biên theo **Luật #1**, đánh dấu clear, báo màu+số lượng đã clear.
- File: `Logic/MatchResolver.cs`.
- **Vai trò:** developer · **Phụ thuộc:** Giai đoạn 1 (data) · **Song song:** Có (cùng 3.1)

**Bước 3.3 — Resolution Loop + nối View**
- `ResolutionLoop`: lặp A→B đến ổn định, phát event mỗi bước để `JuiceFX` tween (scale-pop khi gộp, scale-down + fade khi clear, chain delay). Gọi loop sau mỗi lần drop hợp lệ.
- File: `Logic/ResolutionLoop.cs`, `View/JuiceFX.cs`.
- **Vai trò:** developer · **Phụ thuộc:** 3.1, 3.2, 2.3 · **Song song:** Không

**Bước 3.4 — EditMode Unit Tests cho logic**
- Test các case: gộp 2→1x2, gộp 4→2x2, chéo không gộp, match qua biên 1 hàng, match full-cell, chain nhiều bước, không match khác màu. **Test generator:** FixedQueue trả đúng thứ tự; BagGridAware ưu tiên màu đa số + refill khi rỗng (dùng seed cố định).
- File: `Tests/EditMode/MergeMatchTests.cs`, `Tests/EditMode/DockGeneratorTests.cs` + asmdef.
- **Vai trò:** developer · **Phụ thuộc:** 3.1, 3.2, 2.2 · **Song song:** Có (sau khi logic xong)

## GIAI ĐOẠN 4 — GameManager, Levels, Polish (KHÔNG UI)
**Bước 4.1 — GameManager + GoalTracker + Win/Lose (event-driven)**
- `GameManager` điều phối: load level, lắng nghe clear → `GoalTracker` trừ goal → check **Win** (mọi goal về 0); check **Lose** (lưới không còn slot con trống). Phát các event `OnLevelLoaded/OnGoalsChanged/OnLevelWon/OnLevelLost` để UI ghép sau. Reset/Next level qua API công khai (`LoadLevel(int)`, `RetryLevel()`).
- File: `Gameplay/GameManager.cs`, `Gameplay/GoalTracker.cs`.
- **Vai trò:** developer · **Phụ thuộc:** Giai đoạn 3 · **Song song:** Không

**Bước 4.2 — Debug Overlay tạm (thay UI để kiểm thử)**
- `DebugOverlay` (OnGUI hoặc TextMeshPro world-space): hiển thị Level index, Goals còn lại, trạng thái Win/Lose, nút Next/Retry tạm để test nhanh trong Editor. **Sẽ gỡ/ẩn khi UI thật được ghép vào.**
- File: `View/DebugOverlay.cs`.
- **Vai trò:** developer · **Phụ thuộc:** 4.1 · **Song song:** Có (tách biệt logic)

**Bước 4.3 — Tạo Level 1, 2, 3 (data)**
- `Level_01` (chữ thập, Dock 1 slot, FixedQueue, prefilled như mockup), `Level_02` (4x5, Dock 2 slot, FixedQueue), `Level_03` (kim cương 16 ô, Dock 2 slot, BagGridAware + túi 10 cụm). Đảm bảo giải được.
- File: SO `Level_01`, `Level_02`, `Level_03`.
- **Vai trò:** developer · **Phụ thuộc:** 4.1 · **Song song:** Có (cùng 4.2)

**Bước 4.4 — Polish "Juicy" + Build test Android**
- Tinh chỉnh tween, particle pop khi clear, rung nhẹ camera/haptic. Build APK kiểm tra touch + hiệu năng trên thiết bị thật (chưa có UI vẫn build chạy gameplay được).
- **Vai trò:** developer · **Phụ thuộc:** 4.1–4.3 · **Song song:** Không

---

# Verification & Testing

## Unit Tests (EditMode — logic thuần, không cần scene)
- Gộp: 2 khối cùng màu kề ngang → 1 khối 1x2; kề dọc → 2x1; 4 khối → 2x2.
- Không gộp: 2 khối cùng màu ở 2 góc chéo; 2 khối khác màu.
- Match qua biên: full-cell cùng màu kề nhau → clear cả hai, +2 goal đúng màu.
- Chain: drop tạo gộp → resize → match → clear → tạo gộp tiếp (kiểm tra loop dừng đúng).
- Ràng buộc: 2 khối chưa gộp trong cùng ô luôn khác màu.
- **Generator (Luật #3):** `FixedQueueGenerator` trả đúng thứ tự đã thiết kế; `BagGridAwareGenerator` ưu tiên cụm chứa màu đa số trên lưới, fallback random khi lưới trống, tự refill khi túi rỗng (seed cố định → kết quả deterministic).
- **Win/Lose:** Goals về 0 → Win; lấp đầy mọi slot con → Lose.

## Manual / Play Mode Checks
- Kéo cụm từ Dock thả đúng ô trống → snap chuẩn; thả sai → trả về Dock.
- Drop vào ô đầy hoặc ô không tồn tại (ngoài shape) → bị từ chối.
- Goals giảm đúng theo màu (xem qua Debug Overlay); đạt đủ → log/Event Win; lấp đầy lưới → log/Event Lose.
- L1 chỉ có 1 slot Dock, L2/L3 có 2 slot; sau khi đặt cụm, slot tự nạp lại.
- L3: quan sát cụm sinh ra có ưu tiên màu đang nhiều trên lưới; dùng hết túi → tự nạp túi mới.
- Chạy đủ L1 (chữ thập), L2 (4x5), L3 (kim cương) không lỗi console.

## Build / Device
- Build APK Android Portrait; kiểm tra: touch drag mượt, raycast chính xác trên DPI cao, framerate ổn định (>=60fps mục tiêu). (Prototype chưa có UI — chỉ kiểm tra gameplay 3D.)

---

# Rủi ro & Khuyến nghị kỹ thuật (Senior notes)
- **Phức tạp nhất = Phase B (match qua biên với khối kích thước hỗn hợp).** Khuyến nghị code logic thuần + unit test trước, view sau (đã phản ánh: Giai đoạn 3 có test riêng).
- **Tách Logic/View** giúp debug và đổi luật match (Luật #1) mà không đụng tới rendering.
- **Sparse Dictionary cho Grid** là chìa khoá để hỗ trợ shape chữ thập/kim cương mà không phát sinh ô thừa.
- **Generator sau interface `IClusterGenerator`**: L1/L2 deterministic dễ test; L3 Bag+Grid-Aware có seed inject được → test ổn định. Dễ thêm chế độ sinh mới sau này.
- **Gameplay event-driven, không phụ thuộc UI**: toàn bộ trạng thái phơi qua event/API → khi bạn cung cấp UI, chỉ subscribe là xong, không phải refactor logic.
- **Lưu ý "Lose theo ô trống":** cần định nghĩa rõ "ô trống" = còn ít nhất 1 sub-slot trống ở 1 ô active bất kỳ. Kiểm tra Lose **sau** khi resolution loop ổn định và **sau** khi đã thử nạp cụm mới.
- Nếu thời gian gấp, **polish (4.4)** là phần có thể cắt giảm trước tiên mà không ảnh hưởng tính "playable".
