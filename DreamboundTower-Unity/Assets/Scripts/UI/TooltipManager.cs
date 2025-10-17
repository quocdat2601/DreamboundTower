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

        // ✅ LOGIC ĐIỀU CHỈNH VỊ TRÍ
        // 1. Kiểm tra nếu tooltip tràn ra ngoài rìa phải màn hình
        if (localPoint.x + tooltipSize.x > canvasRect.rect.width / 2) // canvasRect.rect.width / 2 là cạnh phải của canvas
        {
            // Nếu tràn, dịch tooltip sang trái (pivot 1,0)
            targetLocalPosition.x = localPoint.x; // Không cần offset x ban đầu
            pivot.x = 1; // Pivot sang phải để neo tooltip vào chuột
        }
        else
        {
            // Nếu không tràn, dịch tooltip sang phải (pivot 0,0) với offset
            targetLocalPosition.x = localPoint.x + 15f; // Offset 15f giống bạn đã dùng
            pivot.x = 0;
        }

        // 2. Kiểm tra nếu tooltip tràn ra ngoài rìa trên màn hình
        // Lưu ý: localPoint.y sẽ âm nếu chuột ở nửa dưới Canvas
        if (localPoint.y + tooltipSize.y > canvasRect.rect.height / 2) // canvasRect.rect.height / 2 là cạnh trên của canvas
        {
            // Nếu tràn, dịch tooltip xuống dưới (pivot 0,1)
            targetLocalPosition.y = localPoint.y - 15f; // Offset 15f giống bạn đã dùng, nhưng dịch xuống
            pivot.y = 1; // Pivot lên trên để neo tooltip vào chuột
        }
        else
        {
            // Nếu không tràn, dịch tooltip lên trên (pivot 0,0)
            targetLocalPosition.y = localPoint.y + 15f; // Offset 15f giống bạn đã dùng
            pivot.y = 0;
        }

        // Cập nhật pivot và vị trí
        activeTooltipRect.pivot = pivot;
        activeTooltipRect.localPosition = targetLocalPosition;
    }

    // --- CÁC HÀM CÔNG KHAI ---

    public void ShowItemTooltip(GearItem item)
    {
        if (item == null || itemTooltipPanel == null) return;

        HideAllTooltips();
        activeTooltipRect = itemTooltipRect;
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
        activeTooltipRect = itemTooltipRect;
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
        if (statTextPrefab == null)
        {
            Debug.LogWarning("[TOOLTIP] statTextPrefab is not assigned! Please assign a TextMeshPro prefab in the Inspector.");
            return;
        }
        GameObject statGO = Instantiate(statTextPrefab, statContainer);
        TextMeshProUGUI statText = statGO.GetComponent<TextMeshProUGUI>();

        // Định dạng và hiển thị
        statText.text = $"{statName}: +{value}";
    }
}