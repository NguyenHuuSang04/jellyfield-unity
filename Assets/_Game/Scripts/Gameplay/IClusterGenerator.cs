using System.Collections.Generic;
using UnityEngine;

// 1. Interface chung cho mọi bộ sinh khối
public interface IClusterGenerator
{
    bool TryGetNext(GridManager grid, out ClusterData cluster);
}

// 2. THUẬT TOÁN A: Sinh theo hàng đợi kịch bản cố định (Dùng cho Level 1 & 2)
public class FixedQueueGenerator : IClusterGenerator
{
    private Queue<ClusterData> _queue = new Queue<ClusterData>();

    public FixedQueueGenerator(List<ClusterData> fixedList)
    {
        foreach (var item in fixedList)
        {
            _queue.Enqueue(item);
        }
    }

    public bool TryGetNext(GridManager grid, out ClusterData cluster)
    {
        if (_queue.Count == 0)
        {
            cluster = null;
            return false; // Đã hết khối trong kịch bản xếp sẵn
        }
        cluster = _queue.Dequeue();
        return true;
    }
}

// 3. THUẬT TOÁN B: Sinh theo túi bài + Quét màu thông minh (Dùng cho Level 3 Thử thách)
public class BagGridAwareGenerator : IClusterGenerator
{
    private List<ClusterData> _sourceBag; // Túi 10 cụm khuôn mẫu gốc
    private List<ClusterData> _currentBag = new List<ClusterData>(); // Túi hiện tại để bốc dần

    public BagGridAwareGenerator(List<ClusterData> sourceBag)
    {
        this._sourceBag = sourceBag;
        RefillBag();
    }

    private void RefillBag()
    {
        _currentBag = new List<ClusterData>(_sourceBag);
    }

    public bool TryGetNext(GridManager grid, out ClusterData cluster)
    {
        // Quy tắc 3: Nếu dùng hết túi thì tự động nạp đầy lại bản sao mới
        if (_currentBag.Count == 0) RefillBag();

        // Quy tắc 1 (Grid-Aware): Quét ma trận lưới xem màu nào đang còn trên lưới nhiều nhất
        Dictionary<BlockColor, int> colorCounter = new Dictionary<BlockColor, int>();
        foreach (var cell in grid.ActiveCells)
        {
            foreach (var block in cell.Blocks)
            {
                if (!colorCounter.ContainsKey(block.Color)) colorCounter[block.Color] = 0;
                colorCounter[block.Color]++;
            }
        }

        // Tìm các màu đang chiếm đa số trên lưới
        List<BlockColor> desiredColors = new List<BlockColor>(colorCounter.Keys);
        // Sắp xếp giảm dần màu nào nhiều nhất đứng đầu
        desiredColors.Sort((a, b) => colorCounter[b].CompareTo(colorCounter[a]));

        // Quy tắc 2 (Bag System): Lọc trong túi bài xem cụm khuôn mẫu nào chứa màu phù hợp không
        foreach (var color in desiredColors)
        {
            for (int i = 0; i < _currentBag.Count; i++)
            {
                // Xem cụm này có khối nào trùng màu đang cần không
                bool hasMatchingColor = _currentBag[i].Blocks.Exists(b => b.Color == color);
                if (hasMatchingColor)
                {
                    cluster = _currentBag[i];
                    _currentBag.RemoveAt(i);
                    return true;
                }
            }
        }

        // Biện pháp Fallback: Nếu lưới trống hoặc không lọc được cụm khớp -> Bốc đại ngẫu nhiên trong túi bài
        int randomIndex = Random.Range(0, _currentBag.Count);
        cluster = _currentBag[randomIndex];
        _currentBag.RemoveAt(randomIndex);
        return true;
    }
}