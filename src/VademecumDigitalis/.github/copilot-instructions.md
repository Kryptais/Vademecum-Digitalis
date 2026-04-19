# Copilot Instructions for DSA 5 MAUI App

## Project Overview
You are an expert developer specializing in .NET MAUI, C# 12, and the "Das Schwarze Auge 5" (DSA 5) roleplaying system. The project is a mobile character sheet and inventory manager that prioritizes local data persistence and offline capability. The application follows the MVVM pattern using the CommunityToolkit.Mvvm.

## DSA 5 Domain Knowledge & Rules
Always reference the official rules from the [DSA 5 Regelwiki](https://dsa.ulisses-regelwiki.de) for standard values.

### Core Mechanics
- Attributes: MU, KL, IN, CH, FF, GE, KO, KK.
- Skill Checks: 3D20 rolls against three attributes. Points (FP) are calculated by subtracting failed roll margins from the Skill Value (FW).
- Quality Levels (QS): If a check is successful (FP >= 0), the QS starts at 1. The formula is: QS = floor(FP / 3) + 1. 
- IMPORTANT: The QS is NOT capped at 6. Allow for higher values (QS 7+) to support epic play or massive bonuses.

## Homebrew & Customization Support
The application must be designed with high flexibility to allow "Homebrew" content.
- Support custom Advantages/Disadvantages (Vor- und Nachteile) and Special Abilities (Sonderfertigkeiten).
- Do not hardcode lists of skills or traits. Instead, use extensible data structures (e.g., Base Classes or Interfaces) that allow users to add their own entries with custom modifiers.
- Provide generic modifier logic (e.g., Attribute Bonuses, Skill Modifiers) that can be attached to any item or trait.

## Technical Guidelines
- Architecture: Strict MVVM. Use `[ObservableProperty]` and `[RelayCommand]`.
- UI: XAML for Views. Use `ObservableCollection` for dynamic lists.
- Persistence: Focus on a robust local backup system. Ensure that custom Homebrew data is serialized correctly (JSON or SQLite) alongside official data.
- Naming: Use English for code (classes, methods, variables) but keep German DSA terminology in comments or specific string constants to align with the Regelwiki.
- Dependency Injection: Register all Services and ViewModels in `MauiProgram.cs`.

## Coding Style
- Prefer clean, self-documenting code.
- Implement error handling for calculations to prevent crashes if Homebrew data has missing or malformed values.
- Ensure the UI is responsive and handles large inventories or long lists of custom abilities efficiently.