# Conditions im UI: anlegen + Kontext-UC für Charakterseiten

Dieses Konzept setzt genau deinen Wunsch um:
1. **Beim Erstellen neuer Regeln/Modifikatoren können neue Conditions angelegt werden.**
2. Auf Charakterseiten gibt es ein **wiederverwendbares UC**, mit dem mehrere Zustände gleichzeitig gesetzt werden können.
3. Berechnete Zustände (z. B. Belastung aus Rüstung) sind sichtbar, aber nicht manuell schaltbar.

## 1) Conditions beim Erstellen von Regeln

Neu eingeführtes Modell:
- `RuleConditionDefinition` mit `Key`, `Label`, `Group`, `IsUserToggleable`, `SourceHint`.

Damit kann der Nutzer beim Erstellen eines Modifikators:
- vorhandene Condition auswählen (z. B. `context.environment.mountain`), oder
- neue Condition definieren (Key + Label + Gruppe).

Datei: `src/VademecumDigitalis/Models/RuleEngine/RuleConditionDefinition.cs`.

## 2) UC für Kontext-Zustände über mehrere Seiten

Neu eingeführtes UC:
- `RuleContextSelectorView.xaml` + `.xaml.cs`
- Zwei Bereiche:
  - **Manuell wählbar** (Checkbox)
  - **Berechnet/gesperrt** (🔒, nur Anzeige)

Die Bindings sind bewusst generisch gehalten:
- `ManualStates`
- `DerivedStates`

Dadurch kann dasselbe UC auf Hauptblatt, Talente, Kampf, Zauber etc. eingebunden werden.

Dateien:
- `src/VademecumDigitalis/Controls/RuleContextSelectorView.xaml`
- `src/VademecumDigitalis/Controls/RuleContextSelectorView.xaml.cs`

## 3) Zustandsmodell (inkl. „Gebirge“, „Nass“, „Belastet Stufe 2“)

Neu eingeführt:
- `RuleContextState` (Laufzeitzustand)
- `RuleContextStateSource` (`Manual` / `Derived`)

Beispielzustände sind bereits im `RuleContextViewModel` enthalten:
- Manual: `Gebirge`
- Manual: `Nass`
- Derived: `Belastet (Stufe 2)`

Dateien:
- `src/VademecumDigitalis/Models/RuleEngine/RuleContextState.cs`
- `src/VademecumDigitalis/ViewModels/RuleEngine/RuleContextViewModel.cs`

## 4) Beispiel-Einbindung pro Seite

```xml
<controls:RuleContextSelectorView
    ManualStates="{Binding RuleContext.ManualStates}"
    DerivedStates="{Binding RuleContext.DerivedStates}" />
```

Empfehlung:
- `RuleContext` als gemeinsame Instanz pro Charakter-Session halten.
- Jede Seite zeigt dieselben Zustände, aber kann später seitenabhängig filtern (z. B. Kampfseite priorisiert Kampf-relevante Conditions).
