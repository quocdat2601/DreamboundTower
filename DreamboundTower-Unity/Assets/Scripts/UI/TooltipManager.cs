using UnityEngine;
using TMPro;
using System.Text;
using Presets; // Cần thiết để nhận biết BaseSkillSO và GearItem
using UnityEngine.InputSystem;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [Header("Item Tooltip UI")]
    public GameObject itemTooltipPanel;
    public TextMeshProUGUI itemHeaderNameText;
    public TextMeshProUGUI itemHeaderCostText;
    public Transform statContainer; // Kéo panel "Main" có Grid Layout vào đây
    public GameObject statTextPrefab; // Kéo prefab TextMeshPro cho chỉ số vào đây
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
        // Đảm bảo bạn đã thêm 'using UnityEngine.InputSystem;' ở đầu file
        if (Mouse.current == null) return;

        // Lấy vị trí chuột bằng API mới
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // Tính toán vị trí mới với một khoảng đệm
        Vector2 offset = new Vector2(15f, 15f); // 15 pixel sang phải, 15 pixel lên trên
        Vector2 targetPosition = mousePosition + offset;

        if (itemTooltipPanel.activeSelf)
        {
            itemTooltipRect.position = targetPosition;
        }

        if (skillTooltipPanel.activeSelf)
        {
            skillTooltipRect.position = targetPosition;
        }
    }

    // --- CÁC HÀM CÔNG KHAI ---

    public void ShowItemTooltip(GearItem item)
    {
        if (item == null || itemTooltipPanel == null) return;

        HideAllTooltips();

        // Cập nhật Header
        itemHeaderNameText.text = item.itemName;
        itemHeaderCostText.text = item.basePrice.ToString();

        // Dọn dẹp và tạo các dòng chỉ số mới
        PopulateStats(item);

        // Cập nhật mô tả
        if (!string.IsNullOrEmpty(item.description))
        {
            itemDescriptionText.gameObject.SetActive(true);
            itemDescriptionText.text = item.description;
        }
        else
        {
            itemDescriptionText.gameObject.SetActive(false);
        }

        itemTooltipPanel.SetActive(true);
    }

    // Hàm này sẽ được gọi từ BattleUIManager sau khi refactor
    public void ShowSkillTooltip(BaseSkillSO skill, StatBlock characterStats)
    {
        if (skill == null || skillTooltipPanel == null) return;

        HideAllTooltips();
        skillTooltipPanel.SetActive(true);

        if (tooltipSkillName) tooltipSkillName.text = skill.displayName;

        if (skill is SkillData activeSkill)
        {
            // Sử dụng TooltipFormatter với characterStats được truyền vào
            if (tooltipDescription) tooltipDescription.text = $"<b>ACTIVE:</b> {TooltipFormatter.GenerateDescription(activeSkill, characterStats)}";

            if (tooltipManaCostBar) tooltipManaCostBar.SetActive(true);
            if (tooltipCooldownBar) tooltipCooldownBar.SetActive(true);
            if (tooltipManaCostValue) tooltipManaCostValue.text = activeSkill.cost.ToString();
            if (tooltipCooldownValue) tooltipCooldownValue.text = activeSkill.cooldown.ToString();
        }
        else if (skill is PassiveSkillData passiveSkill)
        {
            if (tooltipDescription) tooltipDescription.text = $"<b>PASSIVE:</b> {passiveSkill.descriptionTemplate}";
            if (tooltipManaCostBar) tooltipManaCostBar.SetActive(false);
            if (tooltipCooldownBar) tooltipCooldownBar.SetActive(false);
        }
    }

    public void HideAllTooltips()
    {
        if (itemTooltipPanel != null) itemTooltipPanel.SetActive(false);
        if (skillTooltipPanel != null) skillTooltipPanel.SetActive(false);
    }

    // --- CÁC HÀM NỘI BỘ ---

    private void PopulateStats(GearItem item)
    {
        // Dọn dẹp các chỉ số cũ
        foreach (Transform child in statContainer)
        {
            Destroy(child.gameObject);
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