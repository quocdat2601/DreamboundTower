# Hit Effects Implementation Guide

This guide explains how to set up and use the hit effect system in your combat.

## Files Added

1. **CombatEffectManager.cs** - Main manager for all combat visual effects
2. **DamageNumber.cs** - Handles floating damage numbers
3. **DamageNumberSetup.cs** - Helper script for creating damage number prefabs
4. **Character.cs** - Enhanced with visual effect methods

## Setup Instructions

### 1. Create CombatEffectManager GameObject

1. Create an empty GameObject in your battle scene
2. Name it "CombatEffectManager"
3. Add the `CombatEffectManager` component
4. Configure the following fields:
   - **Damage Number Canvas**: Assign your UI Canvas (should be Screen Space - Overlay)

### 2. Create Damage Number Prefab

1. Create an empty GameObject
2. Add the `DamageNumberSetup` component
3. Click "Setup Damage Number" in the context menu
4. Save as prefab in `Resources/CombatEffects/DamageNumber.prefab`
5. Assign this prefab to CombatEffectManager's `damageNumberPrefab` field

### 3. Configure Character Prefabs

For each character (player and enemies):

1. Select the character prefab
2. In the `Character` component:
   - **Character Image**: Assign the UI Image component that displays the character
   - **Is Player**: Check this for the player character only

### 4. Create Effect Prefabs (Optional)

You can create particle effect prefabs for:
- `hitEffectPrefab` - Normal hit effects
- `criticalHitEffectPrefab` - Critical hit effects
- `missEffectPrefab` - Miss effects
- `damageEffectPrefab` - Damage taken effects
- `deathEffectPrefab` - Death effects

## How It Works

### Attack Flow
1. `Character.Attack()` is called
2. `PlayAttackAnimation()` scales the character up briefly
3. `CombatEffectManager.PlayHitEffect()` spawns hit effects
4. `Character.TakeDamage()` is called on target
5. `PlayHitAnimation()` flashes the target red and shakes it
6. `CombatEffectManager.PlayBeingHitEffect()` shows damage numbers

### Visual Effects

**Attack Effects:**
- Character scales up briefly (1.1x scale)
- Hit effect particles spawn at target position

**Being Hit Effects:**
- Character flashes red briefly
- Character shakes position
- Floating damage number appears

**Death Effects:**
- Death particle effect spawns

## Customization

### Adjusting Effect Intensity

In `CombatEffectManager`:
- Effect prefabs can be customized or left empty for no particle effects

### Modifying Damage Numbers

In `DamageNumber`:
- `floatSpeed` - How fast numbers float up
- `lifetime` - How long numbers stay visible
- `floatDistance` - How far numbers float

### Character Animations

In `Character`:
- Modify `PlayHitAnimation()` for different hit effects
- Modify `PlayAttackAnimation()` for different attack effects

## Testing

1. Start a battle
2. Attack an enemy - you should see:
   - Attacker scales up briefly
   - Target flashes red and shakes
   - Damage number floats up

## Troubleshooting

**No effects showing:**
- Check that CombatEffectManager is in the scene
- Verify characterImage is assigned on characters
- Make sure DOTween is properly imported

**Damage numbers not appearing:**
- Check that damageNumberCanvas is assigned
- Verify damageNumberPrefab is assigned
- Ensure canvas is set to Screen Space - Overlay

## Future Enhancements

- Add different effects for different damage types
- Implement critical hit detection and special effects
- Add sound effects integration
- Create more sophisticated particle effects
- Add status effect visual indicators
