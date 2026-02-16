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

1. .NET SDK 8 installieren.
2. MAUI-Workloads installieren: `dotnet workload install maui`.
3. App starten (z. B. Android): `dotnet build -t:Run -f net8.0-android`.
4. Danach Persistenz ergänzen (lokales Speichern/Laden).
