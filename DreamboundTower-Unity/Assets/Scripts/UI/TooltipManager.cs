using Assets.Scripts.Data;
using Presets; // Cần thiết để nhận biết BaseSkillSO và GearItem
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [Header("Item Tooltip UI")]
    public GameObject itemTooltipPanel;
    public TextMeshProUGUI itemHeaderNameText;
    public TextMeshProUGUI itemHeaderCostText;
    public Transform statContainer; // Kéo panel "Main" có Grid Layout vào đây
    public GameObject statTextPrefab; // Kéo prefab TextMeshPro cho chỉ số vào đây
    public GameObject itemDescriptionGO;
    public TextMeshProUGUI itemDescriptionText;
    private RectTransform itemTooltipRect;

    [Header("Skill Tooltip UI")]
    public GameObject skillTooltipPanel;
    public TextMeshProUGUI tooltipSkillName;
    public TextMeshProUGUI tooltipDescription;
    public GameObject tooltipManaCostBar;
    public GameObject tooltipCooldownBar;
    public TextMeshProUGUI tooltipManaCostValue;
    public TextMeshProUGUI tooltipCooldownValue;
    private RectTransform skillTooltipRect;

    private RectTransform activeTooltipRect;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }

        if (itemTooltipPanel != null)
        {
            itemTooltipRect = itemTooltipPanel.GetComponent<RectTransform>();
            itemTooltipPanel.SetActive(false);
        }
        if (skillTooltipPanel != null)
        {
            skillTooltipRect = skillTooltipPanel.GetComponent<RectTransform>();
            skillTooltipPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (activeTooltipRect == null || !activeTooltipRect.gameObject.activeSelf) return; // Chỉ chạy nếu có tooltip đang hiển thị
        
        // Update tooltip position to follow cursor
        PositionTooltipAtCursor();
    }
    
    /// <summary>
    /// Positions the active tooltip at the cursor position with boundary detection
    /// </summary>
    private void PositionTooltipAtCursor()
    {
        if (activeTooltipRect == null || !activeTooltipRect.gameObject.activeSelf) return;
        
        if (Mouse.current == null) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // Lấy thông tin về màn hình và kích thước tooltip
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("TooltipManager cần nằm dưới một Canvas để tính toán vị trí chính xác.");
            return;
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // Chuyển đổi vị trí chuột từ screen space sang local space của Canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePosition, canvas.worldCamera, out Vector2 localPoint);

        // Lấy kích thước của tooltip
        Vector2 tooltipSize = activeTooltipRect.rect.size;

        // Tính toán vị trí mới
        Vector2 targetLocalPosition = localPoint;
        Vector2 pivot = Vector2.zero; // Mặc định hiển thị về phía trên bên phải (pivot 0,0)

        // ✅ LOGIC ĐIỀU CHỈNH VỊ TRÍ - Reduced offset for closer positioning
        float offsetX = 5f; // Small offset to avoid cursor blocking tooltip
        float offsetY = 5f;
        
        // 1. Kiểm tra nếu tooltip tràn ra ngoài rìa phải màn hình
        if (localPoint.x + tooltipSize.x + offsetX > canvasRect.rect.width / 2) // canvasRect.rect.width / 2 là cạnh phải của canvas
        {
            // Nếu tràn, dịch tooltip sang trái (pivot 1,0)
            targetLocalPosition.x = localPoint.x - offsetX; // Position to the left of cursor
            pivot.x = 1; // Pivot sang phải để neo tooltip vào chuột
        }
        else
        {
            // Nếu không tràn, dịch tooltip sang phải (pivot 0,0) với offset nhỏ
            targetLocalPosition.x = localPoint.x + offsetX; // Small offset to the right
            pivot.x = 0;
        }

        // 2. Kiểm tra nếu tooltip tràn ra ngoài rìa trên màn hình
        // Lưu ý: localPoint.y sẽ âm nếu chuột ở nửa dưới Canvas
        if (localPoint.y + tooltipSize.y + offsetY > canvasRect.rect.height / 2) // canvasRect.rect.height / 2 là cạnh trên của canvas
        {
            // Nếu tràn, dịch tooltip xuống dưới (pivot 0,1)
            targetLocalPosition.y = localPoint.y - offsetY; // Position below cursor
            pivot.y = 1; // Pivot lên trên để neo tooltip vào chuột
        }
        else
        {
            // Nếu không tràn, dịch tooltip lên trên (pivot 0,0)
            targetLocalPosition.y = localPoint.y + offsetY; // Small offset above cursor
            pivot.y = 0;
        }

        // Cập nhật pivot và vị trí
        activeTooltipRect.pivot = pivot;
        activeTooltipRect.localPosition = targetLocalPosition;
    }

    // --- CÁC HÀM CÔNG KHAI ---

    public void ShowItemTooltip(GearItem item)
    {
        // Kiểm tra null ban đầu (giữ nguyên)
        if (item == null || itemTooltipPanel == null) return;

        // Ẩn tooltip khác và set active (giữ nguyên)
        HideAllTooltips();
        activeTooltipRect = itemTooltipRect;

        // Apply rarity background to tooltip panel
        Image tooltipPanelImage = itemTooltipPanel.GetComponent<Image>();
        if (tooltipPanelImage == null)
        {
            tooltipPanelImage = itemTooltipPanel.AddComponent<Image>();
        }
        RarityColorUtility.ApplyRarityBackground(tooltipPanelImage, item.rarity);
        
        // Also look for a background child object in tooltip panel
        Transform tooltipBgTransform = itemTooltipPanel.transform.Find("Background");
        if (tooltipBgTransform != null)
        {
            Image tooltipBgImage = tooltipBgTransform.GetComponent<Image>();
            if (tooltipBgImage != null)
            {
                RarityColorUtility.ApplyRarityBackground(tooltipBgImage, item.rarity);
            }
        }

        // Cập nhật Header với rarity color
        if (itemHeaderNameText)
        {
            itemHeaderNameText.text = item.itemName;
            // Apply rarity color to name text
            itemHeaderNameText.color = RarityColorUtility.GetRarityColor(item.rarity);
        }
        if (itemHeaderCostText) itemHeaderCostText.text = item.basePrice.ToString(); // Thêm .ToString() nếu basePrice là số

        // Dọn dẹp và tạo các dòng chỉ số mới (giữ nguyên)
        PopulateStats(item);
        
        // Add rarity name to stats (if needed, can be added as a stat line)
        // For now, we'll add it to the description area or create a separate rarity indicator

        // --- SỬA LOGIC ẨN/HIỆN DESCRIPTION ---
        if (itemDescriptionGO != null) // Kiểm tra GameObject cha
        {
            // Kiểm tra xem description có rỗng hay null không
            if (string.IsNullOrEmpty(item.description))
            {
                // Nếu rỗng -> Ẩn GameObject CHA ("ItemDescription")
                itemDescriptionGO.SetActive(false);
                if (itemDescriptionText != null) itemDescriptionText.text = ""; // Xóa text cũ (an toàn)
            }
            else
            {
                // Nếu có nội dung -> Hiện GameObject CHA
                itemDescriptionGO.SetActive(true);
                // Gán Text vào component con
                if (itemDescriptionText != null) itemDescriptionText.text = item.description;
            }
        }
        // --- KẾT THÚC SỬA ---

        // Hiện Panel chính
        itemTooltipPanel.SetActive(true);

        // QUAN TRỌNG: Buộc tính toán lại layout NGAY LẬP TỨC
        if (itemTooltipRect != null)
        {
            // Phải gọi sau khi đã SetActive và điền hết data
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemTooltipRect);
            
            // Position tooltip after layout rebuild (size might have changed)
            PositionTooltipAtCursor();
        }
        else
        {
            // Position tooltip immediately at cursor if no layout rebuild needed
            PositionTooltipAtCursor();
        }
    }

    // Hàm này sẽ được gọi từ BattleUIManager sau khi refactor
    // Trong TooltipManager.cs

    // Thêm tham số Character caster
    public void ShowSkillTooltip(BaseSkillSO skill, Character caster, RectTransform sourceRect = null) // Thêm caster
    {
        // ... (Code kiểm tra null ban đầu) ...
        if (skill == null || skillTooltipPanel == null || caster == null) // Thêm kiểm tra caster
        {
            HideAllTooltips(); // Ẩn đi nếu thiếu thông tin
            return;
        }

        HideAllTooltips();
        activeTooltipRect = skillTooltipRect;

        // ... (Code hiển thị tên skill, icon, cost, cooldown giữ nguyên) ...
        if (tooltipSkillName) tooltipSkillName.text = skill.displayName;
        
        // Position tooltip immediately at cursor
        PositionTooltipAtCursor();

        // --- XỬ LÝ DESCRIPTION ĐỘNG ---
        if (tooltipDescription != null)
        {
            // Gọi hàm helper mới để xây dựng mô tả
            tooltipDescription.text = BuildSkillDescription(skill, caster);
        }
        // --- KẾT THÚC XỬ LÝ ---

        skillTooltipPanel.SetActive(true);
        if (skillTooltipRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(skillTooltipRect);

        // Cập nhật vị trí (Nếu bạn dùng sourceRect)
        // if (sourceRect != null && activeTooltipRect != null) { /* ... code update position ... */ }
    }// Thêm hàm mới này vào TooltipManager.cs

    private string BuildSkillDescription(BaseSkillSO baseSkill, Character caster)
    {
        // Lấy template gốc
        string description = baseSkill.descriptionTemplate;
        if (string.IsNullOrEmpty(description)) return ""; // Trả về rỗng nếu không có template

        // Ép kiểu sang SkillData để lấy thông tin chi tiết (nếu là skill chủ động)
        if (baseSkill is SkillData skillData)
        {
            // --- Tính toán các giá trị ---

            // Sát thương
            if (skillData.baseDamage > 0 || skillData.scalingPercent > 0)
            {
                int scalingValue = GetStatValue(caster, skillData.scalingStat);
                int scaledAmount = Mathf.RoundToInt(scalingValue * skillData.scalingPercent);
                int totalDamage = skillData.baseDamage + scaledAmount;
                string scalingInfo = $"(+{skillData.scalingPercent * 100f:F0}% {skillData.scalingStat} (+{scaledAmount}))"; // Ví dụ: (+15% INT (+15))
                string damageTypeStr = skillData.isMagicDamage ? "phép" : "vật lý"; // Lấy loại sát thương

                description = description.Replace("{damage}", totalDamage.ToString());
                description = description.Replace("{scaling_info}", scalingInfo);
                description = description.Replace("{damage_type}", damageTypeStr); // Thêm placeholder này nếu cần
            }

            // Khiên (Shield)
            if (skillData.shieldAmount > 0 || skillData.shieldScalingPercent > 0)
            {
                int shieldScalingValue = GetStatValue(caster, skillData.shieldScalingStat); // Ví dụ: MaxHP
                int scaledShieldAmount = Mathf.RoundToInt(shieldScalingValue * skillData.shieldScalingPercent);
                int totalShield = skillData.shieldAmount + scaledShieldAmount;
                string shieldScalingInfo = $"({skillData.shieldScalingPercent * 100f:F0}% {skillData.shieldScalingStat})"; // Ví dụ: (15% MaxHP)

                description = description.Replace("{shield_value}", totalShield.ToString());
                description = description.Replace("{shield_scaling}", shieldScalingInfo);
            }

            // Thời gian hiệu lực (Duration)
            if (skillData.shieldDuration > 0) // Dùng buffDuration nếu có
            {
                description = description.Replace("{duration}", skillData.shieldDuration.ToString());
            }
            else if (skillData.statusEffectDuration > 0) // Hoặc dùng duration riêng cho shield nếu có
            {
                description = description.Replace("{duration}", skillData.statusEffectDuration.ToString());
            }
            // (Thêm else if cho shield duration riêng nếu cần)


            // Phản đòn (Reflect)
            if (skillData.reflectPercent > 0) // Giả sử reflectPercent trong SkillData là dạng 0-1
            {
                description = description.Replace("{reflect_percent}", (skillData.reflectPercent * 100f).ToString("F0")); // Hiển thị dạng %
            }

            // Hồi máu (Heal)
            if (skillData.healAmount > 0 || skillData.healScalingPercent > 0)
            {
                int healScalingValue = GetStatValue(caster, skillData.healScalingStat);
                int scaledHealAmount = Mathf.RoundToInt(healScalingValue * skillData.healScalingPercent);
                int totalHeal = skillData.healAmount + scaledHealAmount;
                // (Thêm placeholder {heal_scaling} nếu cần)

                description = description.Replace("{heal_amount}", totalHeal.ToString());
            }

            // Chi phí (Cost) - Có thể làm động nếu cost thay đổi theo hiệu ứng
            description = description.Replace("{cost}", skillData.cost.ToString());


            // --- Thêm các phần thay thế khác cho các hiệu ứng bạn có ---
            // Ví dụ: Stun chance, Burn intensity,...
            if (skillData.stunChance > 0) description = description.Replace("{stun_chance}", (skillData.stunChance * 100f).ToString("F0"));
            // ...

        }
        // (Có thể thêm else if (baseSkill is PassiveSkillSO) để xử lý tooltip passive)

        // Trả về chuỗi mô tả đã được thay thế
        return description;
    }

    // Hàm helper để lấy giá trị chỉ số từ Character dựa trên Enum StatType
    private int GetStatValue(Character character, StatType stat)
    {
        if (character == null) return 0;
        switch (stat)
        {
            case StatType.STR: return character.attackPower;
            case StatType.DEF: return character.defense;
            case StatType.INT: return character.intelligence;
            case StatType.MANA: return character.mana; // Max MANA
            case StatType.AGI: return character.agility;
            case StatType.HP: return character.maxHP; // Max HP
                                                      // Thêm các case khác nếu StatType của bạn có nhiều hơn
            default: return 0;
        }
    }
    public void HideAllTooltips()
    {
        if (itemTooltipPanel != null) itemTooltipPanel.SetActive(false);
        if (skillTooltipPanel != null) skillTooltipPanel.SetActive(false);
        activeTooltipRect = null;
    }

    // --- CÁC HÀM NỘI BỘ ---

    private void PopulateStats(GearItem item)
    {
        // Dọn dẹp các chỉ số cũ
        foreach (Transform child in statContainer)
        {
            Destroy(child.gameObject);
        }

        // Add rarity information at the top (as a special stat line with rarity color)
        if (item != null && statTextPrefab != null && statContainer != null)
        {
            GameObject rarityGO = Instantiate(statTextPrefab, statContainer);
            TextMeshProUGUI rarityText = rarityGO.GetComponent<TextMeshProUGUI>();
            if (rarityText != null)
            {
                string rarityName = RarityColorUtility.GetRarityName(item.rarity);
                rarityText.text = $"Rarity: {rarityName}";
                rarityText.color = RarityColorUtility.GetRarityColor(item.rarity);
                // Make it slightly larger/bolder to stand out
                rarityText.fontStyle = FontStyles.Bold;
                if (rarityText.fontSize > 0)
                {
                    rarityText.fontSize *= 1.1f;
                }
            }
        }

        // Tạo dòng mới cho mỗi chỉ số khác 0
        AddStatLine("Max HP", item.hpBonus);
        AddStatLine("MANA", item.manaBonus);
        AddStatLine("STR", item.attackBonus);
        AddStatLine("DEF", item.defenseBonus);
        AddStatLine("INT", item.intBonus);
        AddStatLine("AGI", item.agiBonus);
    }

    private void AddStatLine(string statName, int value)
    {
        if (value == 0) return; // Chỉ hiển thị chỉ số khác 0

        // Tạo một đối tượng Text từ prefab
        GameObject statGO = Instantiate(statTextPrefab, statContainer);
        TextMeshProUGUI statText = statGO.GetComponent<TextMeshProUGUI>();

        // Định dạng và hiển thị
        statText.text = $"{statName}: +{value}";
    }
}