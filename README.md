# DownloadPilot

DownloadPilot is een lokaal Windows-programma dat je Downloads-map en andere rommelplekken op je pc slimmer organiseert. Het programma helpt met bestanden sorteren, dubbele bestanden vinden, facturen herkennen, foto's groeperen, game-restanten opsporen, spamkandidaten in mail vinden en veilige opruimacties voorbereiden.

Alles draait lokaal op je eigen computer. DownloadPilot is gemaakt voor mensen die controle willen houden: eerst bekijken, daarna pas verplaatsen, hernoemen of opruimen.

## Downloaden

De nieuwste portable Windows-versie staat op GitHub Releases:

https://github.com/kratje050/Downloadpilot/releases/latest

Download `DownloadPilot-v0.1.0-win-x64.zip`, pak de zip uit en start `DownloadPilot.App.exe`.

## Wat kun je ermee?

### Slimme Downloads-organisatie

- Bewaakt automatisch je Downloads-map of andere gekozen mappen.
- Wacht tot downloads klaar zijn voordat ze worden verwerkt.
- Herkent tijdelijke downloadbestanden zoals `.crdownload`, `.part`, `.tmp` en `.download`.
- Geeft voorstellen voor doelmap en bestandsnaam.
- Verplaatst en hernoemt bestanden zonder bestaande bestanden te overschrijven.
- Heeft een veilige handmatige modus waarin jij eerst akkoord geeft.

### Automatisch mappen maken

DownloadPilot kan automatisch doelmappen voorstellen en aanmaken, bijvoorbeeld:

- `Facturen/Coolblue`
- `Facturen/Ziggo`
- `Afbeeldingen/Screenshots`
- `Afbeeldingen/Vakantie`
- `Documenten/Belasting`
- `Installers/Drivers`
- `Archief/Oude projecten`

Bij facturen probeert DownloadPilot bedrijfsnamen uit PDF-tekst te halen. Bij afbeeldingen kijkt hij naar bestandsnaam, metadata en optionele OCR-tekst om betere mapvoorstellen te maken.

### Facturen, bonnetjes en documenten

- Leest tekst uit PDF-bestanden.
- Herkent facturen, bonnetjes, contracten en belastingdocumenten.
- Probeert datum, bedrijf en documenttype te herkennen.
- Stelt duidelijke bestandsnamen voor, bijvoorbeeld `2026-07-23_Factuur_Coolblue.pdf`.
- Kan een CSV-export maken van gevonden facturen en bonnetjes.

### Foto's en afbeeldingen

- Ondersteunt lokale OCR voor afbeeldingen als Tesseract-taaldata aanwezig is.
- Herkent foto-onderwerpen uit bestandsnamen en OCR-tekst.
- Groepeert vergelijkbare afbeeldingen.
- Waarschuwt voor foto's die mogelijk EXIF- of locatiemetadata bevatten.
- Helpt generieke namen zoals `IMG_0012.jpg` of `screenshot1.png` beter te benoemen.

### Slimme inbox

De slimme inbox verzamelt aandachtspunten op een plek:

- Nieuwe bestanden die nog verwerkt moeten worden.
- Bestanden met lage zekerheid.
- Grote bestanden.
- Mogelijke dubbele bestanden.
- Bestanden die beter eerst in proefmodus gecontroleerd worden.

### Regels en AI-regelmaker

Je kunt zelf regels maken voor terugkerende downloads. Bijvoorbeeld:

```text
Zet alle bonnetjes van Albert Heijn in boodschappen
```

DownloadPilot zet zo'n opdracht om naar een lokale regel. Je kunt regels daarna aanpassen, prioriteit geven en testen.

### Opruimscan

- Zoekt grote en oude bestanden.
- Geeft opruimvoorstellen zonder direct iets weg te gooien.
- Maakt duidelijk waarom iets kandidaat is.
- Werkt samen met herstelpunten en proefmodus.

### Duplicatencontrole

- Vindt exacte duplicaten via SHA-256.
- Vergelijkt bestanden uit huidige mappen en lokale geschiedenis.
- Laat grootte, locatie en reden zien.
- Helpt veilig bepalen welke kopie je wilt houden.

### Quarantine en herstel

- Risicovolle acties kunnen eerst naar quarantine.
- Verplaatste bestanden zijn terug te draaien via geschiedenis.
- DownloadPilot kan herstelpunten maken voordat je grotere acties uitvoert.
- Proefmodus laat zien wat er zou gebeuren zonder bestanden te verplaatsen.

### Game- en mod-restanten

DownloadPilot kan achtergebleven mappen vinden van verwijderde games, launchers en mods. Denk aan restanten in AppData, Documents, Saved Games en launcher-mappen.

Ondersteunde signalen zijn onder andere:

- Steam
- Epic Games
- GOG Galaxy
- EA app en Origin
- Ubisoft Connect
- Battle.net
- Rockstar Games
- Riot Client
- Xbox Games
- Minecraft
- CurseForge
- Vortex
- Mod Organizer 2
- Thunderstore
- r2modman
- Prism Launcher
- MultiMC
- Modrinth
- ATLauncher
- Overwolf
- itch.io
- Playnite

DownloadPilot verwijdert dit niet automatisch. De app toont kandidaten met score, locatie, grootte en advies, zodat je zelf kunt controleren of het echt weg mag.

### Mailfilter voor Gmail, Hotmail, Outlook en IMAP

DownloadPilot kan via IMAP je mailbox scannen op spamkandidaten.

- Gmail-preset met `imap.gmail.com`.
- Hotmail/Outlook-preset met `outlook.office365.com`.
- Eigen IMAP-server mogelijk.
- Scant de laatste berichten tot een gekozen limiet.
- Geeft spamscore en reden per bericht.
- Verplaatst alleen geselecteerde kandidaten naar spam/junk.

Gebruik voor Gmail bij voorkeur een app-wachtwoord. Voor Outlook/Hotmail is vaak een OAuth-token nodig, omdat Microsoft normale wachtwoord-login meestal blokkeert.

### Power-audit

De Power-audit zoekt extra nuttige opruim- en veiligheidskansen:

- Privacygevoelige bestanden zoals wachtwoorden, BSN, bank, belasting of contracten.
- OneDrive/Google Drive conflict-kopieen.
- Foto's met mogelijke EXIF/locatiemetadata.
- Kapotte snelkoppelingen.
- Lege mappen.
- Oude projectmappen.
- Verdachte downloadbronnen via Windows Zone.Identifier.
- Windows-opstartitems.
- App-caches van onder andere browser, Teams, Discord en crashdumps.
- Grote verborgen bestanden, logs, tempbestanden, backups en dumps.
- Oude installer-versies naast nieuwere installers.
- Generieke bestandsnamen die beter hernoemd kunnen worden.
- Backup-doelen op externe schijven.
- Gezondheidsscore voor Downloads.

### Rapporten

- Weekrapport voor recente activiteit.
- Maandrapport voor grotere trends.
- CSV-export voor facturen en bonnetjes.
- Lokale geschiedenis met herstelbare acties.

### Automatische updates via GitHub

Bij het opstarten controleert DownloadPilot automatisch of er een nieuwe release op GitHub staat:

https://github.com/kratje050/Downloadpilot/releases/latest

Als er een nieuwere versie beschikbaar is, krijg je een popup met downloaden en installeren. De updater zoekt naar release-assets met `.exe`, `.msi` of `.zip`. Je kunt automatische updatechecks uitzetten in Instellingen.

## Veiligheid en privacy

- Geen telemetrie.
- Geen cloudopslag.
- Geen verborgen uploads.
- Bestanden blijven lokaal op je computer.
- Geschiedenis en instellingen staan lokaal in `%LocalAppData%/DownloadPilot`.
- Gevoelige instellingen worden op Windows met DPAPI beschermd.
- Opruimacties zijn standaard controleerbaar via proefmodus, quarantine, geschiedenis en herstelpunten.

## Installatie

1. Ga naar https://github.com/kratje050/Downloadpilot/releases/latest.
2. Download de Windows-zip.
3. Pak de zip uit naar bijvoorbeeld `C:\Program Files\DownloadPilot` of je eigen tools-map.
4. Start `DownloadPilot.App.exe`.
5. Doorloop de eerste-startwizard.

Windows kan bij de eerste keer starten een melding tonen omdat de app nog niet digitaal ondertekend is. Kies dan alleen doorgaan als je de download van je eigen GitHub Release hebt gehaald.

## Builden vanaf broncode

Vereisten:

- Windows 10 of 11, 64-bit
- .NET SDK 8.0

Build:

```powershell
dotnet build .\DownloadPilot.sln -c Release
```

Tests:

```powershell
dotnet test .\DownloadPilot.sln -c Release
```

Portable build:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-portable.ps1 -Configuration Release
```

Output:

```text
publish/win-x64/DownloadPilot.App.exe
```

## Projectstructuur

- `DownloadPilot.App`: WPF Windows-interface.
- `DownloadPilot.Core`: modellen, instellingen en interfaces.
- `DownloadPilot.Infrastructure`: SQLite, scanners, OCR/PDF/mail/update-services en bestandsacties.
- `DownloadPilot.Tests`: unit tests voor kernlogica en scanners.
- `docs`: architectuur, database-schema en bekende beperkingen.
- `scripts`: build-scripts.
- `installer`: Inno Setup installer-bestanden.

## Handige documenten

- Architectuur: `docs/ARCHITECTUUR.md`
- Database-schema: `docs/DATABASE_SCHEMA.sql`
- Bekende beperkingen: `docs/KNOWN_LIMITATIONS.md`
- Voorbeeldregels: `samples/voorbeeldregels.json`

## Bekende beperkingen

- OCR voor afbeeldingen vereist lokale Tesseract-taalbestanden in `%LocalAppData%/DownloadPilot/tessdata`.
- OCR op gescande PDF-pagina's is nog beperkt.
- Mailfilter werkt alleen als IMAP en de juiste loginmethode beschikbaar zijn.
- Game-restanten zijn advieskandidaten, geen automatische verwijderlijst.
- De app is nog niet digitaal ondertekend.

## Versie

Huidige release: `v0.1.0`

Laatste release:

https://github.com/kratje050/Downloadpilot/releases/latest
