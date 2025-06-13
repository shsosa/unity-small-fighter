# Rhythm Combat System for Small Fighter

This rhythm-based combat system adds an exciting new dimension to the Small Fighter game by rewarding players for timing their attacks with the beat of background music.

## Features

- **Beat Detection**: Automatically detects beats in your background music
- **Rhythm Combos**: Chain attacks on the beat for increasing damage multipliers
- **Visual Feedback**: Shows when attacks successfully hit on the beat
- **Flexible Setup**: Easy to configure through the RhythmCombatSetup component

## Getting Started

### 1. Basic Setup

1. Add the `RhythmCombatSetup` script to any GameObject in your scene (e.g., the FightManager)
2. Create a Canvas with a UI Image to use as the beat indicator
3. Assign the following in the inspector:
   - Background music clip
   - Beat indicator prefab and parent transform
   - Rhythm hit effect prefab 
   - Rhythm hit text prefab

### 2. Create Required Prefabs

#### Beat Indicator Prefab
1. Create a UI Image in a Canvas
2. Add the `RhythmBeatIndicator` script to it
3. Configure colors and animation settings
4. Save as a prefab

#### Rhythm Hit Effect Prefab
1. Create a GameObject with a SpriteRenderer
2. Add the `RhythmHitEffect` script
3. Configure colors, duration, and scale
4. Save as a prefab

#### Rhythm Hit Text Prefab
1. Create a UI Text (TextMeshProUGUI) in a Canvas
2. Style it with a bold, visible font
3. Position it centrally or where you want combo notifications
4. Save as a prefab

### 3. Adjusting Settings

- **Beat Threshold**: Adjust this value (0.1-1.0) based on your music to fine-tune beat detection
- **Rhythm Window**: How many seconds around a beat is considered "on beat" (0.05-0.25)
- **Combo Multiplier**: Maximum damage multiplier after several consecutive on-beat hits
- **Music Volume**: Adjust to balance with sound effects

## How It Works

1. The `RhythmManager` analyzes the audio spectrum to detect beats in real-time
2. The `RhythmCombatExtension` on each fighter monitors attack timing
3. When a player attacks on the beat:
   - Visual effects show the rhythm hit
   - A combo counter increases
   - Damage multiplier increases up to the maximum
4. Missing the beat resets the combo

## Advanced Configuration

### Beat Detection Fine-Tuning

If beat detection isn't working well with your music:

1. Adjust the `beatThreshold` value:
   - Higher values (0.5-0.8): Only detect strong beats
   - Lower values (0.1-0.3): Detect more subtle beats
   
2. Try different music with clearer beats

### Custom Visual Effects

You can replace the default visual effects with your own by:

1. Creating custom prefabs
2. Assigning them in the RhythmCombatSetup component

## Troubleshooting

- **No beat detection**: Check if your music has clear beats and adjust threshold
- **Rhythm hits not registering**: Make sure fighters have RhythmCombatExtension components
- **Visual effects not showing**: Verify prefabs are properly assigned

Enjoy your new rhythm-based fighting game!
