# Vademecum Digitalis

Dieses Repository enthält eine .NET-MAUI-App in C# für einen digitalen DSA-5-Charakterbogen.

## Aktueller Stand (V1)

- **Ein Charakter** pro App-Sitzung
- Reiter:
  - **Hauptblatt** (Charakterinfos, Hauptattribute, Basiswerte, AP/SchiP)
  - **Vorteile/Nachteile** (Freitext)
  - **Talente** (Freitext)
  - **Kampftalente** (Freitext)
- Daten sind in der UI editierbar (MVVM-Binding)

## Projektstruktur

- `VademecumDigitalis.sln`
- `src/VademecumDigitalis`

## Nächste Schritte

1. .NET SDK 9 installieren.
2. MAUI-Workloads installieren: `dotnet workload install maui-android` (auf macOS zusätzlich iOS/MacCatalyst).
3. App starten (z. B. Android): `dotnet build -t:Run -f net9.0-android`.
4. Danach Persistenz ergänzen (lokales Speichern/Laden).


## Android SDK-Lizenzen (wiederholte Abfrage)

Wenn Visual Studio / `dotnet` bei jedem Build erneut nach Android-SDK-Lizenzen fragt, liegt meist ein wechselnder oder falscher SDK-Pfad vor.

Empfehlung:
- Setze `ANDROID_SDK_ROOT` (oder `ANDROID_HOME`) dauerhaft auf **ein** SDK-Verzeichnis.
- Akzeptiere Lizenzen einmalig über den gleichen SDK-Pfad (`sdkmanager --licenses`).
- Dieses Projekt übernimmt den Pfad nun automatisch in `AndroidSdkDirectory`, damit nicht zwischen verschiedenen SDK-Installationen gewechselt wird.


## Setup (einmal nach dem Klonen)

Um die häufigen Installations-/Workload-Schritte beim ersten Öffnen auf einem neuen PC zu automatisieren, ist im Repo ein kleines Setup-Skript enthalten.

1. Stelle sicher, dass PowerShell als Benutzer mit Rechten ausgeführt wird (für Workload-Installationen ggf. Administratorrechte).
2. Ausführen aus dem Repo-Root:

```powershell
.
\scripts\setup.ps1
```

Das Skript prüft `dotnet --info`, installiert fehlende MAUI-Workloads (android, maui-windows) wenn nötig und führt einen schnellen Build-Check aus.

Hinweis zu Android: Wenn du Android-Targets verwenden willst, installiere zusätzlich die Android SDK Platform-Tools und den Emulator (Android Studio) und sorge dafür, dass `adb` und `emulator` im PATH sind.

