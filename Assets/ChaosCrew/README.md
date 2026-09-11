# Chaos Crew — Prototyp

Social-Deduction-Spiel für 4 Spieler im Hochformat. Drei Mitarbeiter erledigen Aufgaben,
einer sabotiert heimlich. Am Rundenende wird Überwachungsmaterial gesichtet und abgestimmt.

Unity 6000.6.0f1 · URP · neues Input System · **isometrische 3D-Ansicht** · Mobile Portrait.

---

## Starten

1. Projekt in Unity öffnen, eine beliebige Szene laden (z. B. `Assets/Scenes/SampleScene.unity`).
2. **Play** drücken. Mehr ist nicht nötig.
3. Game-View auf ein Hochformat stellen, z. B. `1080 x 1920`.

Das Spiel baut sich beim Start komplett per Code auf (`Bootstrap.cs`,
`RuntimeInitializeOnLoadMethod`): Büro, Möbel, Figuren, Licht, Kamera und UI entstehen zur
Laufzeit aus Unity-Primitiven. Es gibt **keine Prefabs und keine Szenen-Verkabelung**.

Einziges Binär-Asset ist das Key-Art-Bild für den Ladebildschirm:
`Art/Resources/LoadingScreen.jpg`.

## Steuerung

| Eingabe | Wirkung |
|---|---|
| Rundes D-Pad unten links | Bewegen. Ziehen funktioniert auch neben dem Pad. |
| Großer gelber Hand-Knopf | Kontextaktion: Aufgabe öffnen oder geklemmte Tür befreien (halten) |
| Würfel-Knopf | Kiste aufnehmen / abstellen |
| Läufer-Knopf (halten) | Sprinten — verbraucht Energie |
| Pinker Totenkopf | Nur als Saboteur: Sabotage-Menü |
| Zahnrad oben rechts | Einstellungen |
| Tippen auf die Aufgabenliste | Vollständige Aufgabenliste |
| WASD / Pfeiltasten | Zusätzlich am Desktop zum Testen |

## Rundenablauf

Ladebildschirm → Titel → Lobby (füllt sich mit 3 Bots) → Rollenvergabe → Runde (4 min) →
Beweisphase → Abstimmung (25 s) → Ergebnis.

**Crew gewinnt**, wenn alle Aufgaben fertig sind *oder* der Saboteur rausgewählt wird.
**Saboteur gewinnt**, wenn der Chaos-Balken volläuft *oder* er unerkannt bleibt.

Aufgaben senken den Chaos-Balken, Sabotagen heben ihn — beide Siegbedingungen hängen am
selben Balken und ziehen gegeneinander.

### Eigene Mechaniken

* **Energie & Sprint** — der Blitz-Balken oben. Sprinten leert ihn; ist er leer, musst du erst
  verschnaufen. Kaffee kochen füllt ihn wieder auf.
* **Kisten tragen** — im Lager stehen Kisten, im Büro ist eine Abgabezone (grüner Ring). Jede
  abgelieferte Kiste beruhigt das Chaos. Der Saboteur kann Kisten aufnehmen und irgendwo
  liegen lassen — am besten dort, wo keine Kamera hinsieht.
* **Beweisphase** — Positionen werden mit 5 Hz mitgeschnitten. Nur Räume **mit Kamera** lassen
  sich zu Clips machen; `Lager`, `Toiletten` und `Aufzug` sind blinde Flecken. Nach der Runde
  werden die drei interessantesten Momente als abspielbare Clips gezeigt.

Der Saboteur hat zwei Werkzeuge dagegen:

* **Kamera stören** — die Aufnahme im aktuellen Raum wird für 10 s zu Rauschen.
* **Falsche Spur** — die nächste Tat erscheint im Clip unter der Farbe eines anderen Spielers.

Die Bots stimmen danach ab, *was die Aufnahme zu zeigen scheint* — eine gelungene falsche
Spur führt sie also wirklich in die Irre.

## Architektur

```
Assets/ChaosCrew/
  Art/Resources/LoadingScreen.jpg   Key Art für den Ladebildschirm
  Art/Reference/                    Design-Referenz (nicht im Build genutzt)
  Scripts/
    Core/      Bootstrap, GameDirector (Phasen + Tick), MatchState, Config, Settings, RNG
    Art/       Palette, TextureLab (UI-Sprites), SfxSynth (prozedurale Sounds)
    World/     MaterialLab, PropKit, PropLibrary (Möbel), WorldBuilder, CharacterRig, IsoCamera
    Map/       MapData (ASCII-Parser), OfficeMap (1. Map), MapRuntime (Kollision, A*, Türen)
    Play/      TaskSystem, SabotageSystem, HazardSystem, CarrySystem, BotBrain
    Evidence/  EvidenceRecorder (Mitschnitt, Clip-Bau, Verdachts-Score)
    Meeting/   VoteSystem
    Net/       IMatchTransport + LocalTransport (Bots)
    UI/        IconLab (Vektor-Icons), UIKit, UIManager, Screens, MiniGames, VirtualJoystick
  Editor/      AutoCapture (Entwickler-Werkzeug, siehe unten)
```

Die Simulation ist **frei von Unity-Physik und Unity-Random**: Kollision ist Kreis-gegen-Kachel,
Zufall läuft über `DeterministicRng`. Ein Seed reproduziert eine Runde vollständig — die
Voraussetzung dafür, später echtes Netzwerk-Spiel draufzusetzen.

Die 3D-Welt ist reine Präsentation. Die Simulation rechnet weiter in 2D-Kacheln (x = X,
y = Z), `WorldBuilder` und `CharacterRig` lesen diesen Zustand nur aus.

## Erweitern

**Neue Map** — Klasse wie `OfficeMap` anlegen: ASCII-Layout (`#` Wand, `.` Boden, `D` Tür),
Räume als `RectInt` mit Kamera-Flag, Stationen, Spawnpunkte. In `MapCatalog` eintragen. Achtung:
jede begehbare Kachel muss in genau einem Raum-Rechteck liegen, sonst hat sie keine
Kamera-Zuordnung — die Prüfung im Test-Harness fängt das ab.

**Neues Möbelstück** — Methode in `PropLibrary` nach dem Muster der vorhandenen (aus
`PropKit.Box/Part/Cylinder/Sphere/Panel`), dann in `WorldBuilder.DressRooms` platzieren.

**Neue Aufgabe** — Wert in `MiniGameKind`, Unterklasse von `MiniGame` (nur `Build()`
überschreiben, `Succeed()` aufrufen), `case` in `MiniGame.CreateFor`, Station in der Map.

**Neue Sabotage** — Wert in `SabotageKind`, Eintrag in `SabotageSystem.Catalog`, `case` in
`SabotageSystem.Execute`. Menü und Bot-KI lesen den Katalog automatisch.

**Neues Icon** — Wert in `Icon`, Polygonzug in `IconLab.Build`. Additive Schleifen vereinigen
sich, subtraktive stanzen Löcher.

**Echter Online-Multiplayer** — `IMatchTransport` implementieren. Die Schnittstelle deckt
Lobby, Sitzplätze, Ready-Status und den gemeinsamen Seed ab; `GameDirector` spricht nur über
sie. `LocalTransport` bleibt als Offline-/Testmodus erhalten.

## Entwickler-Werkzeug: AutoCapture

`Editor/AutoCapture.cs` fährt das Spiel automatisch durch und schießt Screenshots — gedacht,
um Änderungen am Aussehen zu prüfen, ohne selbst Play zu drücken.

Auslösen: eine Datei `Temp/cc_capture_request.txt` anlegen. Der Editor importiert dann
geänderte Skripte, startet den Play-Modus, spielt Ladebildschirm → Lobby → Runde →
Beweisphase → Abstimmung → Ergebnis durch und legt die Bilder in `Temp/cc_shots/` ab.
Fehler landen in `Temp/cc_capture_log.txt`, ein Durchlauf endet mit `Temp/cc_capture_done.txt`.

Ohne diese Request-Datei tut das Skript nichts. Wer es nicht braucht, kann die Datei löschen.

## Stellschrauben

Balancing steckt in `Core/CCConfig.cs` und in `SabotageSystem.Catalog`. Die Werte sind über
simulierte Runden eingestellt (Bot gegen Bot, 12 Seeds) und ergeben dort:

* Rundendauer: ca. 2–2,5 min unter Bots (mit menschlichem Spieler länger)
* Ausgang: 6× Crew fertig, 6× Chaos voll
* Saboteur wird von den Bots in ca. 2 von 3 Runden erkannt

In den **Einstellungen** lässt sich die eigene Rolle erzwingen (`IMMER CREW` /
`IMMER SABOTEUR`), um beide Seiten gezielt zu testen.

## Bekannte Grenzen

* Multiplayer ist lokal simuliert (1 Mensch + 3 Bots). Netzwerk-Code ist vorbereitet, aber
  nicht implementiert. Die `32ms`-Anzeige im HUD ist entsprechend ein Platzhalter.
* Die 3D-Welt besteht aus Unity-Primitiven mit Flat-Shading. Das ist bewusst so — es hält das
  Projekt asset-frei und erweiterbar —, erreicht aber nicht die Detailtiefe eines gerenderten
  Konzeptbildes. Wer dorthin will, ersetzt die Aufrufe in `PropLibrary` und `CharacterRig`
  durch echte Modelle; der restliche Code ändert sich dabei nicht.
* Kein Chat, keine Persistenz über die Runde hinaus, keine zweite Etage (der Aufzug ist
  Kulisse plus Ziel der Aufzug-Sabotage).
