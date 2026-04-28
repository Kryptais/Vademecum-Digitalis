# Effekt-System Design (mechanisch + fluff)

Diese Spezifikation beschreibt, **wie Effekte von Vorteilen/Nachteilen technisch ausgewertet werden**.

## 1) Zwei Effektarten: Fluff vs. Mechanik

### Fluff-Effekt (nur Beschreibung)
- Beispiel: „Adliges Ansehen“ erhöht gesellschaftlichen Status, ohne festen Zahlenwert.
- Speicherung als Text + Tags, aber **ohne Berechnungsziel**.
- In der UI sichtbar (Charakterbogen/Notizen), in Rechenpipelines ignoriert.

```json
{
  "id": "adliges_ansehen",
  "name": "Adliges Ansehen",
  "effects": [
    {
      "kind": "Narrative",
      "title": "Sozialer Status",
      "description": "In adligen Kreisen wird die Figur bevorzugt behandelt.",
      "tags": ["gesellschaft", "fluff"]
    }
  ]
}
```

### Mechanischer Effekt (rechnet Werte)
- Beispiel: Vorteil erhöht **GS (Geschwindigkeit)**.
- Speicherung mit Zielwert + Operator + Wert + optionalen Bedingungen.

```json
{
  "id": "flink",
  "name": "Flink",
  "maxStufe": 2,
  "effects": [
    {
      "kind": "Modifier",
      "target": "derived.GS",
      "operation": "Add",
      "value": 1,
      "perLevel": true,
      "stacking": "stack",
      "phase": "derived_values"
    }
  ]
}
```

---

## 2) Minimales Effektmodell

```csharp
public enum EffectKind { Narrative, Modifier }
public enum ModifierOp { Add, Multiply, Override, MinCap, MaxCap }

public sealed class RuleEffect
{
    public string Id { get; init; } = string.Empty;
    public EffectKind Kind { get; init; }

    // Narrative
    public string? Title { get; init; }
    public string? Description { get; init; }

    // Modifier
    public string? Target { get; init; }          // z. B. "derived.GS"
    public ModifierOp? Operation { get; init; }   // Add, Multiply, ...
    public decimal? Value { get; init; }          // +1, *1.1, ...
    public bool PerLevel { get; init; }           // Stufenabhängig?
    public string? Phase { get; init; }           // base | derived_values | checks
    public string? Stacking { get; init; }        // stack | highest | replace
    public ConditionGroup? Condition { get; init; }
}
```

Wichtig:
- `Narrative` hat **kein** `Target`.
- `Modifier` braucht immer `Target + Operation + Value`.

---

## 3) Auswertungspipeline

Reihenfolge (deterministisch):
1. **Base-Werte laden** (z. B. GS-Basis aus Spezies/Ausrüstung).
2. Aktive Quellen sammeln (Vorteile, Nachteile, SF, Zustände, Homebrew-Patches).
3. Nur `Modifier`-Effekte in aktuelle Phase übernehmen.
4. Bedingungen prüfen (`Condition`).
5. Nach Priorität anwenden:
   - `Add`
   - `Multiply`
   - `MinCap`/`MaxCap`
   - `Override`
6. Ergebnis + Audit-Log schreiben.

### Audit-Log (Pflicht)
Jeder Rechenschritt bekommt Quelle:
- Regel-ID (`flink`)
- Effekt-ID
- vorher/nachher
- Grund (z. B. „perLevel x2“)

So kann man im UI genau sehen, **warum** GS z. B. 9 statt 7 ist.

---

## 4) Konkretes Beispiel: Vorteil erhöht GS

Annahme:
- Basis GS: 8
- Vorteil „Flink“ Stufe II (`Add +1 perLevel`)

Berechnung:
- Effektwert = `1 * 2 = +2`
- Neue GS = `8 + 2 = 10`

Audit:
- `derived.GS: 8 -> 10`
- Quelle: `flink`, Effekt `add_gs`, Stufe `2`

---

## 5) Stacking-Regeln (wichtig für Balance)

Vorschlag:
- `stack`: alle Effekte addieren sich (z. B. mehrere kleine Boni).
- `highest`: nur stärkster Effekt zählt (typisch bei ähnlichen Buffs).
- `replace`: Homebrew/Override ersetzt offiziellen Wert vollständig.

Damit verhindert ihr „doppelte“ Boni bei regeltechnisch ähnlichen Effekten.

---

## 6) Bedingungen (optional, aber vorbereitet)

Beispiele:
- nur mit Rüstungstyp X
- nur bei Tag/Nacht
- nur in Zustand „Beritten"

JSON-Skizze:
```json
{
  "condition": {
    "all": [
      { "fact": "context.isMounted", "op": "eq", "value": true }
    ]
  }
}
```

---

## 7) Integration ins bestehende Projekt

Pragmatischer Weg in kleinen Schritten:
1. `VorteilNachteil` um `Effects: List<RuleEffect>` erweitern.
2. Bestehende `ProbenModifikatoren` intern als `RuleEffect(Modifier)` abbilden.
3. Neue `EffectResolver`-Klasse einführen (liefert Endwert + Audit-Liste).
4. Im Charakterbogen „Wertaufschlüsselung“ anzeigen (z. B. GS-Tooltip).

So bleiben alte Daten kompatibel, aber neue Vorteile können sofort saubere Effekte nutzen.
