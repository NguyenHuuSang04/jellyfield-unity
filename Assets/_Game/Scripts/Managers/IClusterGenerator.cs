using System.Collections.Generic;
using UnityEngine;
using JellyField.Core;
using JellyField.Level;

namespace JellyField.Managers
{
    // 1. Interface chung cho mọi bộ sinh khối
    public interface IClusterGenerator
    {
        bool TryGetNext(GridManager grid, out ClusterData cluster);
    }

    // 2. THUẬT TOÁN ĐỘC NHẤT: Sinh theo hàng đợi kịch bản cố định tuần tự (Áp dụng cho mọi Level)
    public class FixedQueueGenerator : IClusterGenerator
    {
        private Queue<ClusterData> _queue = new Queue<ClusterData>();

        public FixedQueueGenerator(List<ClusterData> fixedList)
        {
            if (fixedList != null)
            {
                foreach (var item in fixedList)
                {
                    _queue.Enqueue(item);
                }
            }
        }

        public bool TryGetNext(GridManager grid, out ClusterData cluster)
        {
            if (_queue.Count == 0)
            {
                cluster = null;
                return false; // Đã hết khối trong kịch bản xếp sẵn của LevelData
            }
            cluster = _queue.Dequeue();
            return true;
        }
    }
}