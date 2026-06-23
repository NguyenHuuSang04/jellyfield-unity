using UnityEngine;
using DG.Tweening;

namespace JellyField.View
{
    public class JellyJiggle : MonoBehaviour
    {
        [Header("Cấu Hình Lực Nẩy Thạch")]
        public float bounceDuration = 0.45f;

        [Header("Hiệu Ứng Hạt Bổ Sung (Juicy Particle)")]
        //Ô biến kéo thả Prefab hạt ngoài Inspector của bạn
        public ParticleSystem jellySplashPrefab;

        private Vector3 originalScale;
        private Vector3 currentBendVector;
        private MeshRenderer meshRenderer;
        private Tween jiggleTween; // Quản lý luồng tween duy nhất để chống tràn bộ nhớ

        // Thuộc tính thông minh: Luôn luôn lấy đúng thực thể Material đang hiển thị trên màn hình
        private Material ActiveMaterial
        {
            get
            {
                if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();
                return meshRenderer != null ? meshRenderer.material : null;
            }
        }

        void Start()
        {
            originalScale = transform.localScale;

            // Khởi tạo an toàn cho Shader về trạng thái phẳng ban đầu
            if (ActiveMaterial != null)
            {
                ActiveMaterial.SetVector("_BendOffset", Vector3.zero);
            }
        }

        public void SetBaselineScale(Vector3 newScale)
        {
            originalScale = newScale;
            transform.localScale = newScale;
        }


        // 1. HIỆU ỨNG KHI ĐANG KÉO (Chống nghẽn luồng kéo thả)
        public void UpdateJiggleOnDrag(float dragSpeed, Vector3 dragDirection)
        {
            Material mat = ActiveMaterial;
            if (mat == null) return;

            if (dragSpeed < 0.05f)
            {
                // Nếu dừng tay hoặc di chuyển siêu chậm: Trả Shader về vị trí cân bằng phẳng
                if (jiggleTween != null && jiggleTween.IsActive()) return;

                jiggleTween = DOTween.To(() => currentBendVector, x =>
                {
                    currentBendVector = x;
                    mat.SetVector("_BendOffset", currentBendVector);
                }, Vector3.zero, 0.15f).SetEase(Ease.OutQuad);
                return;
            }

            // Tính toán độ uốn cong đỉnh thạch dựa theo tốc độ vuốt chuột
            float bendIntensity = Mathf.Clamp(dragSpeed * 0.08f, 0f, 0.4f);
            Vector3 localDir = transform.InverseTransformDirection(dragDirection.normalized);
            Vector3 targetBend = new Vector3(localDir.x * bendIntensity, 0f, localDir.z * bendIntensity);

            // : Xóa ngay tween của frame trước để giải phóng CPU trước khi tạo lệnh mới
            if (jiggleTween != null && jiggleTween.IsActive()) jiggleTween.Kill();

            jiggleTween = DOTween.To(() => currentBendVector, x =>
            {
                currentBendVector = x;
                mat.SetVector("_BendOffset", currentBendVector);
            }, targetBend, 0.1f).SetEase(Ease.OutQuad);
        }

        // 2. HIỆU ỨNG KHI THẢ TAY / ĐẶT XUỐNG LƯỚI (Nẩy lò xo Elastic)
        public void PlayDropJiggle()
        {
            Material mat = ActiveMaterial;
            if (mat == null) return;

            if (jiggleTween != null && jiggleTween.IsActive()) jiggleTween.Kill();

            // Tạo lực ép bẹp đỉnh thạch xuống trục Y khi vừa tiếp đất
            Vector3 impactSquash = new Vector3(0f, -0.3f, 0f);
            mat.SetVector("_BendOffset", impactSquash);
            currentBendVector = impactSquash;

            // Sử dụng OutElastic để tạo cú lắc rinh rích rực rỡ từ đỉnh khối thạch
            jiggleTween = DOTween.To(() => currentBendVector, x =>
            {
                currentBendVector = x;
                mat.SetVector("_BendOffset", currentBendVector);
            }, Vector3.zero, bounceDuration).SetEase(Ease.OutElastic);
        }

        //  3. HIỆU ỨNG KHI CÓ Ô TRỐNG: Nẩy bùng nổ tràn ra như nước (Liquid Splash)
        public void PlayMergeJiggle()
        {
            Material mat = ActiveMaterial;
            if (mat == null) return;

            if (jiggleTween != null && jiggleTween.IsActive()) jiggleTween.Kill();

            // Thay vì nảy dọc Y, ép đỉnh thạch lún nhẹ xuống (-0.2f) 
            // và bành trướng bùng nổ sang hai bên trục X và Z (0.35f) để tạo lực tràn lòng!
            Vector3 splashVector = new Vector3(0.35f, -0.2f, 0.35f);
            mat.SetVector("_BendOffset", splashVector);
            currentBendVector = splashVector;

            // Dùng Ease.OutElastic để đỉnh thạch dập dình như sóng nước lan tỏa rồi tắt dần
            jiggleTween = DOTween.To(() => currentBendVector, x =>
            {
                currentBendVector = x;
                mat.SetVector("_BendOffset", currentBendVector);
            }, Vector3.zero, 0.6f).SetEase(Ease.OutElastic);
        }

        private void OnDestroy()
        {
            // Dọn dẹp luồng tween khi đối tượng bị tiêu hủy để tránh rò rỉ RAM
            if (jiggleTween != null && jiggleTween.IsActive()) jiggleTween.Kill();
        }


        //  4. HIỆU ỨNG MATCH TINH GỌN: Phóng to nhẹ rồi thu nhỏ nổ biến mất (KÈM HẠT ĐỒNG MÀU)
        public void PlayMatchExplosionFX(System.Action onCompleteCallback)
        {
            Material mat = ActiveMaterial;

            // Dọn dẹp các luồng hoạt họa cũ để tránh xung đột cấu hình
            if (jiggleTween != null && jiggleTween.IsActive()) jiggleTween.Kill();
            transform.DOKill();

            // Phóng to nhẹ khối thạch lên 1.2 lần mốc gốc để tạo đà nổ
            Vector3 popUpScale = originalScale * 1.2f;

            // Cho đỉnh Shader nhô lên một chút tạo lực dập dình
            if (mat != null)
            {
                Vector3 pushUp = new Vector3(0f, 0.25f, 0f);
                mat.SetVector("_BendOffset", pushUp);
                currentBendVector = pushUp;

                jiggleTween = DOTween.To(() => currentBendVector, x =>
                {
                    currentBendVector = x;
                    mat.SetVector("_BendOffset", currentBendVector);
                }, Vector3.zero, 0.22f).SetEase(Ease.OutElastic);
            }

            // CHUỖI DIỄN HOẠT: Phóng to nhanh (0.12s) -> Giật thu nhỏ nổ biến mất (0.15s)
            transform.DOScale(popUpScale, 0.12f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                // Thu nhỏ đột ngột về mốc 0 bằng Ease.InBack tạo nhịp vỡ nổ dứt khoát
                transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    // THÊM VÀO: Kích nổ hệ thống hạt Juicy Particle đồng màu tuyệt đối
                    if (jellySplashPrefab != null)
                    {
                        ParticleSystem fxInstance = Instantiate(jellySplashPrefab, transform.position, Quaternion.identity);
                        
                        // Ép hệ thống hạt dùng chung cục Material màu sắc bóng bẩy với thạch hiện tại
                        ParticleSystemRenderer psr = fxInstance.GetComponent<ParticleSystemRenderer>();
                        if (psr != null && mat != null)
                        {
                            psr.material = mat; 
                        }

                        // Tự động giải phóng bản sao FX khỏi RAM sau khi phun xong chùm hạt
                        var mainModule = fxInstance.main;
                        Destroy(fxInstance.gameObject, mainModule.duration + mainModule.startLifetime.constantMax);
                    }

                    // Gọi lệnh Callback giải phóng khối thạch ra khỏi bàn cờ tuần tự
                    onCompleteCallback?.Invoke();
                });
            });
        }
    }
}