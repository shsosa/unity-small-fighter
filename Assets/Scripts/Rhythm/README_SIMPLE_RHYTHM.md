# Simple Rhythm Combat System

This is a completely standalone rhythm combat system for Unity Small Fighter that doesn't depend on AudioVisualizer or any external packages.

## Quick Start Guide

1. Add the `SimpleRhythmBootstrapper` component to **any GameObject** in your scene
2. That's it! The system will automatically:
   - Create a rhythm detection system
   - Set up UI elements to visualize beats
   - Add rhythm combat extensions to all fighters

## How It Works

### Beat Detection
The system uses BPM (beats per minute) to precisely track beats. When a fighter attacks exactly on a beat:
- The fighter will flash yellow
- A hit effect will appear
- Damage will be increased by the combo multiplier
- Combo counter will increase

### Combo System
Each successive hit on-beat increases your combo counter and damage multiplier:
- First hit: x1.0 damage
- Second hit: x1.1 damage
- Third hit: x1.2 damage
- And so on, up to the maximum (default is 2.0x)

Missing a beat will reset your combo.

## Components

### SimpleRhythmBootstrapper
Add this to any GameObject to set up the entire system.

Properties:
- **Music Clip**: Optional music clip to use (will create a simple beat if none provided)
- **BPM**: Beats per minute of the music (default: 120)
- **Beat Window**: How close to a beat the player must attack (in seconds)
- **Damage Multiplier**: Base damage multiplier for on-beat attacks
- **Max Combo Multiplier**: Maximum damage multiplier from combos
- **Combo Increment**: How much the multiplier increases per combo hit

### SimpleRhythmSystem
Core system that detects beats and provides events.

### SimpleRhythmFighter
Added automatically to each fighter to enable rhythm combat mechanics.

## Customization

You can modify the following settings in the SimpleRhythmBootstrapper:

- **BPM**: Adjust to match your music
- **Beat Window**: Make this larger for easier timing, smaller for more challenge
- **Damage Multipliers**: Adjust these to balance gameplay
- **Visual Feedback**: Modify colors and effects as needed

## Troubleshooting

If the system isn't working:

1. Check the Console for error messages
2. Make sure the BPM matches your music
3. Try increasing the Beat Window to make detection more forgiving
4. Confirm the fighters have the NewFighter component

## Notes

- This system is completely self-contained and doesn't rely on AudioVisualizer
- It creates all needed UI elements automatically
- The beat detection is based on precise timing rather than audio analysis
- Debug information is shown in the top-left corner
