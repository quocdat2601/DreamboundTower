using UnityEngine;
using UnityEngine.UI;

public class StarRow : MonoBehaviour
{
    [Header("3 image sao theo thứ tự trái→phải")]
    public Image[] starImages;     // Kéo 3 Image (con) vào đây
    [Header("Sprites")]
    public Sprite starOff;
    public Sprite starOn;

    void Awake() => ResetOff();

    public void ResetOff()
    {
        if (starImages == null) return;
        for (int i = 0; i < starImages.Length; i++)
        {
            var img = starImages[i];
            if (!img) continue;
            img.color = Color.white;              // đề phòng alpha = 0
            img.sprite = starOff;
            img.overrideSprite = null;            // clear override
            //img.SetNativeSize();                // bật nếu muốn kích thước theo sprite off
        }
        Debug.Log("[StarRow] ResetOff() called");
    }

    public void Apply(bool cond1, bool cond2, bool cond3)
    {
        ResetOff();
        if (starImages == null || starImages.Length < 3)
        {
            Debug.LogError("[StarRow] starImages null hoặc < 3");
            return;
        }

        bool[] conds = { cond1, cond2, cond3 };
        for (int i = 0; i < 3; i++)
        {
            var img = starImages[i];
            if (!img) { Debug.LogError($"[StarRow] starImages[{i}] is null"); continue; }

            if (conds[i])
            {
                img.color = Color.white;          // chắc chắn hiển thị
                img.sprite = starOn;
                img.overrideSprite = starOn;      // chống case bị state UI đè
                //img.SetNativeSize();            // bật nếu muốn đổi size theo sprite on
                Debug.Log($"[StarRow] turn ON star {i} (cond={conds[i]})");
            }
            else
            {
                Debug.Log($"[StarRow] keep OFF star {i} (cond={conds[i]})");
            }
        }
    }
}
