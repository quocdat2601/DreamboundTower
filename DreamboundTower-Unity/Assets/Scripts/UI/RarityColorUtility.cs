using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Utility class for managing rarity colors and backgrounds
/// </summary>
public static class RarityColorUtility
{
    // Rarity Colors (RGB values normalized 0-1)
    private static readonly Color CommonColor = new Color(0.75f, 0.75f, 0.75f, 1f);      // Light Gray (#BFBFBF)
    private static readonly Color UncommonColor = new Color(0.2f, 0.8f, 0.2f, 1f);       // Green (#33CC33)
    private static readonly Color RareColor = new Color(0.2f, 0.5f, 1f, 1f);             // Blue (#3366FF)
    private static readonly Color EpicColor = new Color(0.8f, 0.2f, 1f, 1f);              // Purple (#CC33FF)
    private static readonly Color LegendaryColor = new Color(1f, 0.843f, 0f, 1f);         // Gold (#FFD700)

    // Background Colors (slightly darker/more transparent for backgrounds)
    private static readonly Color CommonBgColor = new Color(0.65f, 0.65f, 0.65f, 0.3f);   // Light Gray with transparency
    private static readonly Color UncommonBgColor = new Color(0.1f, 0.6f, 0.1f, 0.3f);   // Green with transparency
    private static readonly Color RareBgColor = new Color(0.1f, 0.4f, 0.8f, 0.3f);       // Blue with transparency
    private static readonly Color EpicBgColor = new Color(0.6f, 0.1f, 0.8f, 0.3f);       // Purple with transparency
    private static readonly Color LegendaryBgColor = new Color(0.9f, 0.7f, 0f, 0.4f);    // Gold with transparency

    // Border Colors (for slot borders)
    private static readonly Color CommonBorderColor = new Color(0.7f, 0.7f, 0.7f, 1f);   // Gray border
    private static readonly Color UncommonBorderColor = new Color(0.2f, 0.7f, 0.2f, 1f);  // Green border
    private static readonly Color RareBorderColor = new Color(0.2f, 0.5f, 0.9f, 1f);     // Blue border
    private static readonly Color EpicBorderColor = new Color(0.7f, 0.2f, 0.9f, 1f);     // Purple border
    private static readonly Color LegendaryBorderColor = new Color(1f, 0.8f, 0f, 1f);    // Gold border

    /// <summary>
    /// Gets the primary color for a rarity (for text/icons)
    /// </summary>
    public static Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => CommonColor,
            ItemRarity.Uncommon => UncommonColor,
            ItemRarity.Rare => RareColor,
            ItemRarity.Epic => EpicColor,
            ItemRarity.Legendary => LegendaryColor,
            _ => CommonColor
        };
    }

    /// <summary>
    /// Gets the background color for a rarity (for slot/tooltip backgrounds)
    /// </summary>
    public static Color GetRarityBackgroundColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => CommonBgColor,
            ItemRarity.Uncommon => UncommonBgColor,
            ItemRarity.Rare => RareBgColor,
            ItemRarity.Epic => EpicBgColor,
            ItemRarity.Legendary => LegendaryBgColor,
            _ => CommonBgColor
        };
    }

    /// <summary>
    /// Gets the border color for a rarity (for slot borders)
    /// </summary>
    public static Color GetRarityBorderColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => CommonBorderColor,
            ItemRarity.Uncommon => UncommonBorderColor,
            ItemRarity.Rare => RareBorderColor,
            ItemRarity.Epic => EpicBorderColor,
            ItemRarity.Legendary => LegendaryBorderColor,
            _ => CommonBorderColor
        };
    }

    /// <summary>
    /// Gets the rarity name as a string
    /// </summary>
    public static string GetRarityName(ItemRarity rarity)
    {
        return rarity.ToString();
    }

    /// <summary>
    /// Applies rarity background color to an Image component
    /// ONLY changes the color, preserves all other Image properties (type, sprite, etc.)
    /// </summary>
    public static void ApplyRarityBackground(Image image, ItemRarity rarity)
    {
        if (image == null) return;
        
        Color rarityColor = GetRarityBackgroundColor(rarity);
        
        // ONLY change the color - preserve all other properties (type, sprite, etc.)
        image.color = rarityColor;
        
        // Don't change Image Type, sprite, or other properties to avoid deformation
        // The slot's original configuration (Sliced, etc.) should be preserved
    }

    /// <summary>
    /// Applies rarity border color to an Image component (for borders)
    /// </summary>
    public static void ApplyRarityBorder(Image image, ItemRarity rarity)
    {
        if (image == null) return;
        image.color = GetRarityBorderColor(rarity);
    }
}

