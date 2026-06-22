using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    public static LevelData ChosenLevelData;

    [Header(" Thư Viện Lưu Trữ Các Màn Chơi Asset")]
    public List<LevelData> menuLevels = new List<LevelData>();

    [Header(" Quản Lý Thành Phần UI Ngoài Menu")]
    public Button btnPlay; 
    public List<Button> levelButtons = new List<Button>(); 

    // Màu tối (Xám mờ 50%) đại diện cho trạng thái chưa được chọn
    private Color dimmedColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    void Start()
    {

        //  KHÓA & LÀM XÁM NÚT PLAY QUA COLOR BLOCK
        if (btnPlay != null)
        {
            btnPlay.interactable = false;
            
            ColorBlock playColors = btnPlay.colors;
            playColors.disabledColor = dimmedColor; // Ép màu khóa thành xám tối
            btnPlay.colors = playColors;

            Image playImage = btnPlay.GetComponent<Image>();
            if (playImage != null) playImage.color = dimmedColor; 
        }


        //  ÉP XÁM TOÀN BỘ NÚT LEVEL KHI VỪA VÀO GAME (FIX TRÙNG MÀU)
        // Sửa tận gốc bằng cách can thiệp vào Normal Color của hệ thống Button Tint
        foreach (var btn in levelButtons)
        {
            if (btn != null)
            {
                btn.transform.localScale = Vector3.one;
                
                // Can thiệp ColorBlock để Unity không thể tự động đè lại màu trắng
                ColorBlock cb = btn.colors;
                cb.normalColor = dimmedColor;
                cb.highlightedColor = Color.white; // Cho phép sáng lên khi di chuột vào
                btn.colors = cb;

                Image btnImage = btn.GetComponent<Image>();
                if (btnImage != null) btnImage.color = dimmedColor; 
            }
        }

        ChosenLevelData = null;
    }


    //  XỬ LÝ CLICK CHỌN LEVEL: Đổi màu ColorBlock động cực mượt
    public void OnLevelSelected(int levelNumber)
    {
        int selectedIndex = levelNumber - 1; 

        if (menuLevels != null && selectedIndex >= 0 && selectedIndex < menuLevels.Count)
        {
            ChosenLevelData = menuLevels[selectedIndex];
            Debug.Log($"[MainMenuManager]: Đang chọn trước dữ liệu -> LEVEL {levelNumber}");

            //  SÁNG NÚT PLAY
            if (btnPlay != null)
            {
                btnPlay.interactable = true;
                
                ColorBlock playColors = btnPlay.colors;
                playColors.normalColor = Color.white;
                btnPlay.colors = playColors;

                Image playImage = btnPlay.GetComponent<Image>();
                if (playImage != null)
                {
                    playImage.transform.DOKill();
                    playImage.DOColor(Color.white, 0.2f);
                }

                btnPlay.transform.DOKill();
                btnPlay.transform.localScale = Vector3.one;
                btnPlay.transform.DOScale(new Vector3(1.08f, 1.08f, 1.08f), 0.15f).SetLoops(2, LoopType.Yoyo);
            }

            // VÒNG LẶP ĐIỀU CHỈNH BỘ BA NÚT LEVEL 
            for (int i = 0; i < levelButtons.Count; i++)
            {
                if (levelButtons[i] == null) continue;

                ColorBlock cb = levelButtons[i].colors;
                Image btnImage = levelButtons[i].GetComponent<Image>();
                
                levelButtons[i].transform.DOKill();
                if (btnImage != null) btnImage.transform.DOKill();

                if (i == selectedIndex)
                {
                    //  NÚT ĐƯỢC CHỌN:  sáng Color Block + Phóng to nẩy Outback!
                    cb.normalColor = Color.white;
                    levelButtons[i].colors = cb;
                    
                    if (btnImage != null) btnImage.DOColor(Color.white, 0.25f);
                    levelButtons[i].transform.DOScale(Vector3.one * 1.16f, 0.3f).SetEase(Ease.OutBack);
                }
                else
                {
                    //  NÚT CÒN LẠI: Trở về màu xám tối Color Block + Thu nhỏ về phom gốc
                    cb.normalColor = dimmedColor;
                    levelButtons[i].colors = cb;
                    
                    if (btnImage != null) btnImage.DOColor(dimmedColor, 0.2f);
                    levelButtons[i].transform.DOScale(Vector3.one, 0.25f);
                }
            }
        }
    }

    public void OnPlayPressed()
    {
        if (ChosenLevelData != null)
        {
            Debug.Log($"[MainMenuManager]: Vào trận màn chơi thực tế: LEVEL {ChosenLevelData.LevelIndex}");
            SceneManager.LoadScene("GameplayScene");
        }
    }
}