using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GimmickDatabase", menuName = "Presets/GimmickDatabase", order = 100)]
public class GimmickDatabaseSO : ScriptableObject
{
    // Một danh sách chứa tất cả các Gimmick và mô tả của chúng
    public List<GimmickDescriptionEntry> gimmickDescriptions;
}