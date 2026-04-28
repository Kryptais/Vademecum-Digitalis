# DSA-Regelwerksnahe Neuausrichtung – Arbeitsplan

## Zielbild
- Die Regeln im Backend orientieren sich stärker an DSA5 (Regelwiki-Struktur statt rein statischer Listen).
- Sonderfertigkeiten, Vor-/Nachteile, Talente und Kampftechniken werden als regelbasierte Objekte mit Voraussetzungen, Effekten und Modifikatoren modelliert.
- Homebrew ist **first-class**: Offizielle Regelbasis bleibt nachvollziehbar, kann aber pro Regelobjekt übersteuert oder erweitert werden.
- Möglichst jeder numerische Wert ist modifizierbar (Basiswert + Modifikator-Pipeline + Herkunftsnachweis).

---

## 1) Regelwiki-Research (fachliche Basis)

### 1.1 Sonderfertigkeiten: Hauptgruppen + sinnvolle Untergruppen
**Regelwiki-Hauptgruppen (DSA):**
- Profane Sonderfertigkeiten
- Magische Sonderfertigkeiten
- Karmale Sonderfertigkeiten
- Tierische Sonderfertigkeiten

**Wichtige Unterstrukturen für die Modellierung:**
- Kampfsonderfertigkeiten: passive Manöver, Basismanöver, Spezialmanöver; optionale/fokusregelabhängige Varianten.
- Stil-Systeme: Kampfstilsonderfertigkeiten, Talentstilsonderfertigkeiten inkl. „erweiterte“ Stil-SFs.
- Tradition/Quelle-Kontext: z. B. Traditionsartefakt-SFs, institutionelle Lernquellen, Publikationsbindung.

**Plan-Output:** Taxonomie-Tabelle, die jede SF in `Kategorie -> Unterkategorie -> Tag-Set` überführt.

### 1.2 Vor- und Nachteile (deep dive)
**Zu erfassende Mechaniken:**
- AP-Ökonomie in der Erschaffung (Kauf/Limit/Einmaligkeit/Ausnahmen).
- Erwerbslogik außerhalb Erschaffung (in der Regel nicht frei erlernbar, Ausnahmen explizit).
- Modifikationstypen: statisch, situativ, zustandsabhängig, triggerbasiert.

**Plan-Output:** Effektbibliothek für Vorteile/Nachteile (`Add`, `Multiply`, `Override`, `Conditional`, `OncePerScene` etc.).

### 1.3 Talente und Kampftechniken
**Talente:**
- FW/FP/QS-Logik, Probenarten, Aktiv-/Inaktiv-Status (bei übernatürlichen Fertigkeiten).
- Anwendungsgebiete und Spezialisierungen als regeltechnische Subobjekte.

**Kampftechniken:**
- Leiteigenschaft(en), Steigerungsfaktor, Interaktion mit AT/PA/FK/TP.
- Manöverfähigkeit über SF-Voraussetzungen und Fokusregeln.

**Plan-Output:** gemeinsames „Fertigkeitssystem“ mit Spezialisierungen + technikspezifischen Regeln.

---

## 2) Domänenmodell (Backend-Architektur)

### 2.1 Kernobjekte (neu/erweitert)
- `RuleEntity` (Basistyp): `Id`, `Name`, `Source`, `Version`, `Tags`, `Enabled`.
- `SpecialAbility` (Sonderfertigkeit): Kategorie, Unterkategorie, Kosten, Voraussetzungen, Effekte.
- `Trait` (Vorteil/Nachteil): Typ, AP-Wert, Erwerbsregeln, Effekte.
- `Skill` (Talent/Zauber/Liturgie) und `CombatTechnique`.
- `Modifier` (zentrale Einheit): Zielwert, Operation, Betrag/Formel, Bedingung, Priorität, Herkunft.

### 2.2 Werteberechnung als Pipeline
**Formelprinzip:**
`Endwert = Base + Sum(Add) -> Multiplikatoren -> Caps/Floors -> Overrides`

**Designregeln:**
- Jeder Schritt auditierbar (`AppliedModifiers[]` mit Quelle).
- Reihenfolge deterministisch (Priorität + Domänenphase).
- Konfliktlösung bei mehrfachen Overrides über Prioritätsstufen.

### 2.3 Voraussetzungen-/Prerequisite-Engine
- Ausdrucksbasierte Voraussetzungen (`hasSF`, `attribute>=X`, `tradition==...`, `skill>=...`).
- Laufende Validierung für Erwerb und Anwendung.
- Soft- vs. Hard-Requirements (Hinweis vs. Blocker).

---

## 3) Homebrew-Architektur

### 3.1 Regelquellen trennen
- `official/*` (Regelwiki-konforme Baseline)
- `homebrew/*` (eigene Erweiterungen)

### 3.2 Merge-Strategie
- `add`: neues Objekt ergänzen.
- `patch`: bestehendes Objekt partiell ändern.
- `replace`: bewusst vollständige Übersteuerung.
- `disable`: Regelobjekt deaktivieren.

### 3.3 Governance/UI-Transparenz
- Jede Abweichung markiert als „Homebrew“ mit Delta-Ansicht.
- Import/Export als JSON für Kampagnen-Sharing.
- Optionales Preset-System pro Gruppe/Kampagne.

---

## 4) Umsetzungs-Roadmap (iterativ)

### Phase A – Analyse & Mapping (1–2 Iterationen)
1. Bestehende JSON-Modelle prüfen (`profane_sf.json`, `vorteile_nachteile.json`).
2. Taxonomie-Mapping gegen Regelwiki erstellen.
3. Lückenliste (fehlende Felder/Voraussetzungen/Effektarten) erstellen.

### Phase B – Engine-Fundament (2–3 Iterationen)
1. Einheitliches `Modifier`-Modell einführen.
2. Berechnungs-Pipeline für abgeleitete Werte + Talente + Kampftechniken implementieren.
3. Prerequisite-Engine integrieren.

### Phase C – Datenmigration (2–4 Iterationen)
1. Offizielle Datensätze in neues Schema migrieren.
2. Regressionstests für AP-Kosten, Voraussetzungen, Manöverlogik.
3. Rückwärtskompatible Importpfade bereitstellen.

### Phase D – Homebrew-Layer (2 Iterationen)
1. Merge-Engine und Delta-Anzeige.
2. UI-Schalter „Official / Official+Homebrew“.
3. Validierungsregeln (Warnungen bei inkonsistenten Overrides).

### Phase E – Stabilisierung
1. Balancing-Checks (Stacking, Exploits, cap-breaking).
2. Snapshot-Tests für typische DSA-Charaktere.
3. Dokumentation für Maintainer + Nutzer.

---

## 5) Teststrategie
- **Unit:** Formeloperationen, Prerequisite-Ausdrücke, Merge-Fälle.
- **Golden Tests:** Beispielcharaktere mit erwarteten Endwerten.
- **Property-based:** Modifikator-Reihenfolge bleibt deterministisch.
- **Migration Tests:** alte JSON rein, neue Entities raus (verlustfrei oder mit erklärten Deltas).

---

## 6) Offene Entscheidungen für nächste Abstimmung
1. Zielumfang nur DSA5-Kernregeln oder inkl. Fokusregeln/Kompendien als Standard?
2. Homebrew priorisiert auf Gruppenebene oder pro Charakter separat?
3. Sollen „Publikation + Seitenreferenz“ als Pflichtmetadaten gespeichert werden?
4. Bevorzugst du eher „regelstrikt“ (Blocker bei Verstößen) oder „narrativ flexibel“ (Warnungen)?

---

## 7) Start-Backlog (konkret für den nächsten Austausch)
1. Dateninventur der aktuellen Models/Services im Repo.
2. Zielschema v1 (C#-Klassen + JSON-Schema) vorschlagen.
3. Ein vertikaler Spike: **1 Vorteil + 1 Nachteil + 1 Kampf-SF + 1 Talentmodifikator** Ende-zu-Ende.
4. Danach Review mit dir und Anpassung der Prioritäten.
