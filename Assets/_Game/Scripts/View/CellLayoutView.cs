using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using JellyField.Core;

namespace JellyField.View
{
    public class CellLayoutView : MonoBehaviour
    {
        private Vector2Int GetFirstSlot(JellyBlock b)
        {
            foreach (var s in b.LocalSlots) return s;
            return Vector2Int.zero;
        }

        private void AnimateResize(Transform targetTransform, Vector3 targetLocalPos, Vector3 targetLocalScale, float duration)
        {
            if (targetTransform == null) return;

            // Dọn dẹp luồng tween cũ tránh xung đột kích thước
            targetTransform.DOKill();

            // 1. Di chuyển tâm vị trí mượt mà vào vùng ô lưới mới
            targetTransform.DOLocalMove(targetLocalPos, duration).SetEase(Ease.OutQuad);

            // 2. Ép khối thạch bùng nổ phình to tràn ra lòng ô trống như chất lỏng bằng Ease.OutElastic
            targetTransform.DOScale(targetLocalScale, duration * 1.5f).SetEase(Ease.OutElastic);
        }

        public void NormalizeCellLayout(GridCell cell, bool animate = false)
        {
            if (cell == null || cell.Blocks == null || cell.Blocks.Count == 0) return;

            if (GridManager.Instance == null) return;

            if (animate)
            {
                foreach (var b in cell.Blocks)
                {
                    var visuals = GridManager.Instance.GetVisuals(b.Id);
                    if (visuals != null)
                    {
                        foreach (var vObj in visuals)
                        {
                            if (vObj != null)
                            {
                                JellyJiggle jiggle = vObj.GetComponent<JellyJiggle>();
                                if (jiggle != null) jiggle.PlayMergeJiggle();
                            }
                        }
                    }
                }
            }

            float size = GridManager.Instance.CellSize;
            float maxSubBlockSize = size / 2f;
            float gap = size * GridManager.Instance.CellGapPercent;
            int count = cell.Blocks.Count;

            if (count == 1)
            {
                var b = cell.Blocks[0];
                b.LocalSlots = new HashSet<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1) };
                Vector3 targetPos = new Vector3(0f, 0f, 0f);
                Vector3 targetScale = new Vector3(size - gap, 1f, size - gap);

                var visuals = GridManager.Instance.GetVisuals(b.Id);
                if (visuals != null)
                {
                    foreach (var vObj in visuals)
                    {
                        if (vObj == null) continue;

                        JellyJiggle jiggle = vObj.GetComponent<JellyJiggle>();
                        if (jiggle != null) jiggle.SetBaselineScale(targetScale);

                        if (animate) AnimateResize(vObj.transform, targetPos, targetScale, 0.3f);
                        else { vObj.transform.localPosition = targetPos; vObj.transform.localScale = targetScale; }
                    }
                }
            }
            else if (count == 2)
            {
                var b0 = cell.Blocks[0]; var b1 = cell.Blocks[1];
                Vector2Int slot0 = GetFirstSlot(b0);
                Vector2Int slot1 = GetFirstSlot(b1);

                bool isVerticalStack = (slot0.x == slot1.x);

                if (isVerticalStack)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var b = cell.Blocks[i];
                        int originY = GetFirstSlot(b).y;
                        float offsetZ = (originY == 0) ? -size / 4f : size / 4f;
                        int ySlot = (originY == 0) ? 0 : 1;
                        b.LocalSlots = new HashSet<Vector2Int> { new Vector2Int(0, ySlot), new Vector2Int(1, ySlot) };
                        Vector3 targetPos = new Vector3(0f, 0f, offsetZ);
                        Vector3 targetScale = new Vector3(size - gap, 1f, maxSubBlockSize - gap);

                        var visuals = GridManager.Instance.GetVisuals(b.Id);
                        if (visuals != null)
                        {
                            foreach (var vObj in visuals)
                            {
                                if (vObj == null) continue;

                                JellyJiggle jiggle = vObj.GetComponent<JellyJiggle>();
                                if (jiggle != null) jiggle.SetBaselineScale(targetScale);

                                if (animate) AnimateResize(vObj.transform, targetPos, targetScale, 0.3f);
                                else { vObj.transform.localPosition = targetPos; vObj.transform.localScale = targetScale; }
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var b = cell.Blocks[i];
                        int originX = GetFirstSlot(b).x;
                        float offsetX = (originX == 0) ? -size / 4f : size / 4f;
                        int xSlot = (originX == 0) ? 0 : 1;
                        b.LocalSlots = new HashSet<Vector2Int> { new Vector2Int(xSlot, 0), new Vector2Int(xSlot, 1) };
                        Vector3 targetPos = new Vector3(offsetX, 0f, 0f);
                        Vector3 targetScale = new Vector3(maxSubBlockSize - gap, 1f, size - gap);

                        var visuals = GridManager.Instance.GetVisuals(b.Id);
                        if (visuals != null)
                        {
                            foreach (var vObj in visuals)
                            {
                                if (vObj == null) continue;

                                JellyJiggle jiggle = vObj.GetComponent<JellyJiggle>();
                                if (jiggle != null) jiggle.SetBaselineScale(targetScale);

                                if (animate) AnimateResize(vObj.transform, targetPos, targetScale, 0.3f);
                                else { vObj.transform.localPosition = targetPos; vObj.transform.localScale = targetScale; }
                            }
                        }
                    }
                }
            }
            else if (count == 3)
            {
                bool[,] occupied = new bool[2, 2];
                foreach (var b in cell.Blocks)
                {
                    Vector2Int s = GetFirstSlot(b);
                    occupied[Mathf.Clamp(s.x, 0, 1), Mathf.Clamp(s.y, 0, 1)] = true;
                }

                int emptyX = 0, emptyY = 0;
                for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++)
                        if (!occupied[x, y]) { emptyX = x; emptyY = y; }

                bool chooseHorizontalExpand = (emptyX == emptyY);

                foreach (var b in cell.Blocks)
                {
                    Vector2Int s = GetFirstSlot(b);
                    int slotX = s.x; int slotY = s.y;
                    float offsetX = (slotX == 0) ? -size / 4f : size / 4f;
                    float offsetZ = (slotY == 0) ? -size / 4f : size / 4f;
                    float scaleX = maxSubBlockSize - gap; float scaleZ = maxSubBlockSize - gap;
                    b.LocalSlots = new HashSet<Vector2Int> { new Vector2Int(slotX, slotY) };

                    if (chooseHorizontalExpand)
                    {
                        if (slotX == 1 - emptyX && slotY == emptyY)
                        {
                            offsetX = 0f; scaleX = size - gap; b.LocalSlots.Add(new Vector2Int(emptyX, emptyY));
                        }
                    }
                    else
                    {
                        if (slotX == emptyX && slotY == 1 - emptyY)
                        {
                            offsetZ = 0f; scaleZ = size - gap; b.LocalSlots.Add(new Vector2Int(emptyX, emptyY));
                        }
                    }

                    Vector3 targetPos = new Vector3(offsetX, 0f, offsetZ);
                    Vector3 targetScale = new Vector3(scaleX, 1f, scaleZ);

                    var visuals = GridManager.Instance.GetVisuals(b.Id);
                    if (visuals != null)
                    {
                        foreach (var vObj in visuals)
                        {
                            if (vObj == null) continue;

                            JellyJiggle jiggle = vObj.GetComponent<JellyJiggle>();
                            if (jiggle != null) jiggle.SetBaselineScale(targetScale);

                            if (animate) AnimateResize(vObj.transform, targetPos, targetScale, 0.3f);
                            else { vObj.transform.localPosition = targetPos; vObj.transform.localScale = targetScale; }
                        }
                    }
                }
            }
            else if (count == 4)
            {
                foreach (var b in cell.Blocks)
                {
                    Vector2Int s = GetFirstSlot(b);
                    int slotX = Mathf.Clamp(s.x, 0, 1); int slotY = Mathf.Clamp(s.y, 0, 1);
                    float offsetX = (slotX == 0) ? -size / 4f : size / 4f;
                    float offsetZ = (slotY == 0) ? -size / 4f : size / 4f;
                    b.LocalSlots = new HashSet<Vector2Int> { new Vector2Int(slotX, slotY) };
                    Vector3 targetPos = new Vector3(offsetX, 0f, offsetZ);
                    Vector3 targetScale = new Vector3(maxSubBlockSize - gap, 1f, maxSubBlockSize - gap);

                    var visuals = GridManager.Instance.GetVisuals(b.Id);
                    if (visuals != null)
                    {
                        foreach (var vObj in visuals)
                        {
                            if (vObj == null) continue;

                            JellyJiggle jiggle = vObj.GetComponent<JellyJiggle>();
                            if (jiggle != null) jiggle.SetBaselineScale(targetScale);

                            if (animate) AnimateResize(vObj.transform, targetPos, targetScale, 0.3f);
                            else { vObj.transform.localPosition = targetPos; vObj.transform.localScale = targetScale; }
                        }
                    }
                }
            }
        }
    }
}