using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Presets;
using StatusEffects;

public class EnemyInfoPanel : MonoBehaviour
{
    [Header("Database")]
    public GimmickDatabaseSO gimmickDatabase;

    [Header("Phần 1: Header")]
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI enemyHPText;

    [Header("Phần 2: Stats")]
    public TextMeshProUGUI hpStatText;
    public TextMeshProUGUI strStatText;
    public TextMeshProUGUI manaStatText;
    public TextMeshProUGUI defStatText;
    public TextMeshProUGUI intStatText;
    public TextMeshProUGUI agiStatText;

    [Header("Phần 3: Gimmicks")]
    [Tooltip("Kéo GameObject GimmickPanel vào đây")]
    public GameObject gimmickPanelGO; // GameObject cha của phần Gimmick
    [Tooltip("Kéo Transform con của GimmickPanel (chứa VLG) vào đây")]
    public Transform gimmickContentParent; // Nơi spawn prefab
    public GameObject gimmickDescPrefab;

    [Header("UI Control")] // Thêm header này cho rõ ràng
    [Tooltip("Kéo BlockerPanel (Image nền mờ) vào đây")]
    public GameObject blockerPanel; // <-- THÊM BIẾN NÀY
    public Button closeButton;

    private List<GameObject> spawnedGimmickObjects = new List<GameObject>();

    void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
        gameObject.SetActive(false);
    }

    private void ClearPanel()
    {
        // Dọn dẹp prefab gimmick cũ
        foreach (GameObject gimmickObj in spawnedGimmickObjects) Destroy(gimmickObj);
        spawnedGimmickObjects.Clear();

        // Luôn ẩn GimmickPanel theo mặc định
        if (gimmickPanelGO != null) gimmickPanelGO.SetActive(false);
    }

    public void DisplayEnemyInfo(Character character, EnemyTemplateSO template)
    {
        if (character == null || template == null)
        {
            HidePanel();
            return;
        }

        ClearPanel();
        gameObject.SetActive(true);
        if (blockerPanel != null)
        {
            blockerPanel.SetActive(true); // <-- THÊM VÀO: Hiện lớp nền mờ
        }

        // --- Đổ dữ liệu Header và Stats ---
        enemyNameText.text = template.name;
        enemyHPText.text = $" HP: {character.currentHP} / {character.maxHP}";
        hpStatText.text = $"HP: {character.maxHP}";
        strStatText.text = $"STR: {character.attackPower}";
        manaStatText.text = $"MANA: {character.mana}";
        defStatText.text = $"DEF: {character.defense}";
        intStatText.text = $"INT: {character.intelligence}";
        agiStatText.text = $"AGI: {character.agility}";
        // --- Xử lý Gimmick ---
        PopulateGimmicks(character, template);
    }

    private void PopulateGimmicks(Character character, EnemyTemplateSO template)
    {
        if (gimmickDatabase == null || gimmickDescPrefab == null || gimmickPanelGO == null || gimmickContentParent == null) return;

        bool hasGimmick = false;
        foreach (var entry in gimmickDatabase.gimmickDescriptions)
        {
            if (template.gimmick.HasFlag(entry.gimmick))
            {
                // Chỉ cần bật GimmickPanel nếu tìm thấy Gimmick đầu tiên
                if (!hasGimmick)
                {
                    gimmickPanelGO.SetActive(true);
                    hasGimmick = true;
                }

                // Spawn prefab vào Transform con (nơi có VLG)
                GameObject row = Instantiate(gimmickDescPrefab, gimmickContentParent);
                spawnedGimmickObjects.Add(row);

                string descText = $"<b>{entry.gimmickName}</b>: {entry.description}";
                // ... (code xử lý Resurrect status...)
                if (entry.gimmick == EnemyGimmick.Resurrect)
                {
                    var resurrect = character.GetComponent<ResurrectBehavior>();
                    if (resurrect != null && resurrect.hasResurrected)
                    {
                        descText += " <color=#FF5555>(Đã dùng)</color>";
                    }
                }

                row.GetComponentInChildren<TextMeshProUGUI>().text = descText;
            }
        }

        // Nếu không có gimmick nào, đảm bảo panel bị tắt (đã làm trong ClearPanel)
        // if (!hasGimmick) { gimmickPanelGO.SetActive(false); } // Dòng này không cần thiết nữa
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
        if (blockerPanel != null)
        {
            blockerPanel.SetActive(false); // <-- THÊM VÀO: Ẩn lớp nền mờ
        }
    }
}