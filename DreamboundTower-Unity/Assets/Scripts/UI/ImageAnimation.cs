using UnityEngine;
using System.Collections;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

public class ImageAnimation : MonoBehaviour {

	[SerializeField]
	private Sprite[] sprites;
	
	[Tooltip("Animation speed in frames per second (e.g., 12 = 12 FPS animation)")]
	[Range(1, 60)]
	public float animationFPS = 12f; // Animation FPS (not game FPS)
	
	[Tooltip("Drag sprite sheet Texture2D here to auto-load all sprites, OR drag individual sprites directly into the Sprites array below")]
	public Texture2D spriteSheet;
	
	public bool loop = true;
	public bool destroyOnEnd = false;
	
	// Public property to get sprite count (for duration calculation)
	public int SpriteCount => sprites != null ? sprites.Length : 0;
	
	// Get animation duration in seconds
	public float Duration => SpriteCount > 0 ? SpriteCount / animationFPS : 0f;

	private int index = 0;
	private Image image;
	private float timer = 0f;

	void Awake() {
		image = GetComponent<Image> ();
	}
	
	void Start() {
		// Set first sprite immediately
		if (sprites != null && sprites.Length > 0 && image != null) {
			image.sprite = sprites[0];
		}
	}

	void Update () {
		if (sprites == null || sprites.Length == 0) return;
		if (image == null) return;
		
		// If animation finished and not looping, stop updating
		if (!loop && index >= sprites.Length) return;
		
		// Time-based animation: calculate time per frame
		float timePerFrame = 1f / animationFPS;
		timer += Time.deltaTime;
		
		// Check if it's time to advance to next sprite
		if (timer >= timePerFrame) {
			// Reset timer (carry over excess time for smooth animation)
			timer -= timePerFrame;
			
			// Update sprite
			image.sprite = sprites[index];
			
			// Advance to next sprite
			index++;
			
			// Handle loop or destroy
			if (index >= sprites.Length) {
				if (loop) {
					index = 0; // Loop back to start
				} else {
					// Reached end, check if should destroy
					if (destroyOnEnd) {
						Destroy(gameObject);
						return;
					}
					// Don't advance beyond last sprite
					index = sprites.Length;
				}
			}
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Automatically populates sprites array from sprite sheet when spriteSheet is assigned
	/// </summary>
	void OnValidate() {
		if (spriteSheet != null)
		{
			LoadSpritesFromSheet();
		}
	}

	void LoadSpritesFromSheet() {
		if (spriteSheet == null) return;

		string path = AssetDatabase.GetAssetPath(spriteSheet);
		if (string.IsNullOrEmpty(path)) return;

		// Load all sprites from the sprite sheet
		Sprite[] loadedSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
			.OfType<Sprite>()
			.OrderBy(s => {
				// Try to sort by name (sprite-sheet_0, sprite-sheet_1, etc.)
				string name = s.name;
				int lastUnderscore = name.LastIndexOf('_');
				if (lastUnderscore >= 0 && lastUnderscore < name.Length - 1)
				{
					string numberPart = name.Substring(lastUnderscore + 1);
					if (int.TryParse(numberPart, out int num))
					{
						return num;
					}
				}
				return 0;
			})
			.ToArray();

		if (loadedSprites != null && loadedSprites.Length > 0)
		{
			sprites = loadedSprites;
			// Optional: Clear spriteSheet reference after loading to avoid re-loading
			// spriteSheet = null;
			Debug.Log($"Loaded {loadedSprites.Length} sprites from sprite sheet: {spriteSheet.name}");
		}
	}
#endif
}

