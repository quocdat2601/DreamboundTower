using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Represents a dropped item in the world that can be picked up
/// </summary>
public class LootPickup : MonoBehaviour
{
    [Header("Loot Data")]
    public GearItem item;
    public int quantity = 1;
    public ItemRarity rarity = ItemRarity.Common;
    
    [Header("Visual Settings")]
    [Tooltip("Should the item glow based on rarity?")]
    public bool useRarityGlow = true;
    
    [Tooltip("Should the item bob up and down?")]
    public bool useBobbingAnimation = true;
    
    [Tooltip("Speed of bobbing animation")]
    public float bobbingSpeed = 2f;
    
    [Tooltip("Height of bobbing animation")]
    public float bobbingHeight = 0.2f;
    
    [Tooltip("Should the item pulse/glow to attract attention?")]
    public bool usePulseGlow = true;
    
    [Tooltip("Speed of pulsing glow effect")]
    public float pulseSpeed = 3f;
    
    [Tooltip("Intensity of the pulse glow")]
    public float pulseIntensity = 0.5f;
    
    [Header("Collection")]
    [Tooltip("Can this item be collected by clicking?")]
    public bool clickToCollect = true;
    
    [Tooltip("Auto-collect after this many seconds (0 = disabled)")]
    public float autoCollectDelay = 0f;
    
    // Components
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D pickupCollider;
    private Vector3 startPosition;
    private float despawnTime = 60f;
    private float spawnTime;
    private bool isAutoCollecting = false;
    
    
    // Visual enhancement components
    private GameObject shadowEffect;
    private SpriteRenderer shadowRenderer;
    
    // Events
    public System.Action<LootPickup> OnCollected;
    public System.Action<LootPickup> OnDespawned;
    
    void Awake()
    {
        // Get required components (assume they're manually set up in prefab)
        spriteRenderer = GetComponent<SpriteRenderer>();
        pickupCollider = GetComponent<CircleCollider2D>();
        
        startPosition = transform.position;
        spawnTime = Time.time;
    }
    
    /// <summary>
    /// Get the time when this loot was spawned
    /// </summary>
    public float SpawnTime => spawnTime;
    
    /// <summary>
    /// Get the remaining time until auto-collect (0 if auto-collect disabled or already passed)
    /// </summary>
    public float RemainingAutoCollectTime()
    {
        if (autoCollectDelay <= 0f)
        {
            return 0f;
        }
        float elapsedTime = Time.time - spawnTime;
        float remaining = autoCollectDelay - elapsedTime;
        return Mathf.Max(0f, remaining);
    }
    
    void Start()
    {
        // Start despawn timer
        if (despawnTime > 0)
        {
            StartCoroutine(DespawnTimer());
        }
        
        // Start auto-collect timer if enabled
        if (autoCollectDelay > 0)
        {
            StartCoroutine(AutoCollectTimer());
        }
    }
    
    void Update()
    {
        // FORCE full opacity every frame to prevent transparency issues
        if (spriteRenderer != null && spriteRenderer.enabled)
        {
            Color currentColor = spriteRenderer.color;
            currentColor.a = 1.0f; // Always force full opacity
            spriteRenderer.color = currentColor;
        }
        
        // Handle bobbing animation
        if (useBobbingAnimation)
        {
            float bobOffset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
            transform.position = startPosition + Vector3.up * bobOffset;
        }
        
        // Handle pulsing glow effect - use scale instead of alpha for better visibility
        if (usePulseGlow && spriteRenderer != null)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * (pulseIntensity * 0.5f); // Reduce pulse intensity by half
            Vector3 baseScaleVector = Vector3.one * 80.0f; // Use the same scale as in UpdateVisuals
            transform.localScale = baseScaleVector * (1f + pulse);
            
            // Ensure alpha stays at full opacity
            Color currentColor = spriteRenderer.color;
            currentColor.a = 1.0f; // Keep alpha at full opacity
            spriteRenderer.color = currentColor;
        }
        
        // Update shadow effect to follow the main item
        if (shadowEffect != null && shadowRenderer != null)
        {
            shadowRenderer.sprite = spriteRenderer.sprite;
            shadowRenderer.enabled = spriteRenderer.enabled;
        }
        
        // Handle click to collect (only if not auto-collecting)
        if (clickToCollect && !isAutoCollecting && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mousePos.z = 0;
            
            if (Vector3.Distance(transform.position, mousePos) <= pickupCollider.radius)
            {
                CollectLoot();
            }
        }
    }
    
    
    void OnMouseDown()
    {
        // Handle click to collect (only if not auto-collecting)
        if (clickToCollect && !isAutoCollecting)
        {
            CollectLoot();
        }
    }
    
    /// <summary>
    /// Setup the loot pickup with item data
    /// </summary>
    /// <param name="gearItem">The item to drop</param>
    /// <param name="minQty">Minimum quantity</param>
    /// <param name="maxQty">Maximum quantity</param>
    /// <param name="itemRarity">Rarity of the item</param>
    public void SetupLoot(GearItem gearItem, int minQty = 1, int maxQty = 1, ItemRarity itemRarity = ItemRarity.Common)
    {
        item = gearItem;
        quantity = Random.Range(minQty, maxQty + 1);
        rarity = itemRarity;
        
        // Ensure quantity is at least 1
        if (quantity <= 0)
        {
            quantity = 1;
        }
        
        // Update visual representation
        UpdateVisuals();
    }
    
    /// <summary>
    /// Set the despawn time for this loot
    /// </summary>
    /// <param name="time">Time in seconds before despawning</param>
    public void SetDespawnTime(float time)
    {
        despawnTime = time;
    }
    
    /// <summary>
    /// Update the visual representation of the loot
    /// </summary>
    void UpdateVisuals()
    {
        if (spriteRenderer == null) return;
        
        // Set the item icon (if item exists)
        if (item != null)
        {
            spriteRenderer.sprite = item.icon;
        }
        else
        {
            spriteRenderer.sprite = null; // No icon for test items
        }
        
        // Scale the loot pickup to make it more visible
        transform.localScale = Vector3.one * 80.0f; // Make all loot items 2x bigger and more visible (doubled from 40.0f)
        
        // Additional scale based on quantity (if more than 1)
        if (quantity > 1)
        {
            transform.localScale *= (1f + (quantity - 1) * 0.1f);
        }
        
        // Ensure the sprite is visible and on top of everything FIRST
        spriteRenderer.enabled = true;
        spriteRenderer.sortingOrder = 32767; // Maximum possible sorting order to appear on top of everything including UI
        spriteRenderer.sortingLayerName = "Default"; // Make sure it's on the default layer
        
        // Force it to be a world space object, not UI
        if (transform.parent != null)
        {
            Canvas parentCanvas = transform.parent.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                transform.SetParent(null); // Move out of any Canvas
            }
        }
        
        // Apply rarity-based coloring with full opacity
        if (useRarityGlow)
        {
            Color rarityColor = GetRarityColor(rarity);
            rarityColor.a = 1.0f; // Ensure full opacity
            spriteRenderer.color = rarityColor;
        }
        else
        {
            spriteRenderer.color = Color.white; // Default white with full opacity
        }
        
        // FORCE full opacity - no transparency at all (this overrides everything)
        Color finalColor = spriteRenderer.color;
        finalColor.a = 1.0f; // Force alpha to 1.0 (fully opaque)
        spriteRenderer.color = finalColor;
        
        // Create a completely opaque material to override any transparency
        Material opaqueMaterial = new Material(Shader.Find("Sprites/Default"));
        opaqueMaterial.color = Color.white;
        spriteRenderer.material = opaqueMaterial;
        
        
        // Also try to set the material's render queue to opaque
        if (spriteRenderer.material.HasProperty("_RenderQueue"))
        {
            spriteRenderer.material.SetFloat("_RenderQueue", 2000); // Opaque render queue
        }
        
        
        // Create shadow effect for better visibility
        if (shadowEffect == null)
        {
            CreateShadowEffect();
        }
        
    }
    
    
    /// <summary>
    /// Create a shadow effect behind the loot for better visibility
    /// </summary>
    void CreateShadowEffect()
    {
        shadowEffect = new GameObject("ShadowEffect");
        shadowEffect.transform.SetParent(transform);
        shadowEffect.transform.localPosition = new Vector3(0.1f, -0.1f, 0.1f); // Offset for shadow
        shadowEffect.transform.localScale = Vector3.one * 1.1f; // Slightly larger
        
        shadowRenderer = shadowEffect.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = spriteRenderer.sprite; // Same sprite as main item
        shadowRenderer.color = new Color(0, 0, 0, 0.3f); // Semi-transparent black
        shadowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1; // Behind the main item
        shadowRenderer.sortingLayerName = "Default";
        shadowRenderer.enabled = true;
    }
    
    /// <summary>
    /// Get color based on item rarity
    /// </summary>
    /// <param name="rarity">The rarity level</param>
    /// <returns>Color for the rarity</returns>
    Color GetRarityColor(ItemRarity rarity)
    {
        Color color;
        switch (rarity)
        {
            case ItemRarity.Common:
                color = Color.white;
                break;
            case ItemRarity.Uncommon:
                color = Color.green;
                break;
            case ItemRarity.Rare:
                color = Color.blue;
                break;
            case ItemRarity.Epic:
                color = Color.magenta;
                break;
            case ItemRarity.Legendary:
                color = Color.yellow;
                break;
            default:
                color = Color.white;
                break;
        }
        
        // Ensure full opacity
        color.a = 1.0f;
        return color;
    }
    
    /// <summary>
    /// Collect this loot item
    /// </summary>
    void CollectLoot()
    {
        if (LootManager.Instance != null)
        {
            LootManager.Instance.CollectLoot(this);
        }
        
        // Trigger collection event
        OnCollected?.Invoke(this);
    }
    
    /// <summary>
    /// Auto-collect timer coroutine
    /// </summary>
    IEnumerator AutoCollectTimer()
    {
        yield return new WaitForSeconds(autoCollectDelay);
        
        // Mark as auto-collecting to prevent manual collection
        isAutoCollecting = true;
        
        // Auto-collect the item
        CollectLoot();
    }
    
    /// <summary>
    /// Despawn timer coroutine
    /// </summary>
    IEnumerator DespawnTimer()
    {
        yield return new WaitForSeconds(despawnTime);
        
        // Trigger despawn event
        OnDespawned?.Invoke(this);
        
        // Remove from LootManager if it exists
        if (LootManager.Instance != null)
        {
            LootManager.Instance.activeLoot.Remove(this);
        }
        
        // Destroy the object
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Force collect this loot (useful for testing)
    /// </summary>
    [ContextMenu("Force Collect")]
    public void ForceCollect()
    {
        CollectLoot();
    }
    
}
