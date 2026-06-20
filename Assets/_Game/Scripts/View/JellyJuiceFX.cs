using UnityEngine;
using DG.Tweening; // Khai báo thư viện DOTween

public static class JellyJuiceFX
{
    // 1. Hiệu ứng khi khối thạch được thả bộp từ trên cao xuống ô lưới (Squash & Stretch)
    public static void PlayDropBounce(Transform targetTransform)
    {
        if (targetTransform == null) return;

        // Reset lại scale gốc trước khi chạy
        targetTransform.localScale = Vector3.one;

        // DOPunchScale(Vector3 punch, float duration, int vibrato, float elasticity)
        // Co bóp mạnh theo trục XZ (ngang) và nảy nhẹ trục Y
        targetTransform.DOPunchScale(new Vector3(0.3f, -0.2f, 0.3f), 0.4f, 10, 1f);
    }

    // 2. Hiệu ứng khi các khối nhỏ hút vào nhau và phình to (Merge)
    public static void PlayMergePop(Transform targetTransform)
    {
        if (targetTransform == null) return;

        // Tạo hiệu ứng phình to lên 1.2 lần rồi co lại về kích thước chuẩn thật nhanh
        targetTransform.localScale = Vector3.one;
        targetTransform.DOScale(new Vector3(1.2f, 1.1f, 1.2f), 0.15f)
            .OnComplete(() => {
                targetTransform.DOScale(Vector3.one, 0.15f);
            });
    }

    // 3. Hiệu ứng nhấp nháy thu nhỏ biến mất khi thạch bị nổ ăn điểm (Match Clear)
    public static void PlayExplodeClear(Transform targetTransform, System.Action onCompleteCallback)
    {
        if (targetTransform == null) {
            onCompleteCallback?.Invoke();
            return;
        }

        // Khối thạch nảy nhẹ lên một cái rồi thu nhỏ biến mất về 0 hoàn toàn
        targetTransform.DOScale(new Vector3(1.3f, 1.3f, 1.3f), 0.1f)
            .OnComplete(() => {
                targetTransform.DOScale(Vector3.zero, 0.2f)
                    .OnComplete(() => {
                        onCompleteCallback?.Invoke(); // Kích hoạt xóa Object thật sau khi diễn hoạt xong
                    });
            });
    }
}