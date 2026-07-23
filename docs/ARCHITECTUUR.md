# DownloadPilot Architectuur

## Projecten
- DownloadPilot.App: WPF UI, MVVM, applicatiestart, systeemvak en logging-configuratie.
- DownloadPilot.Core: domeinmodellen, enumtypes en interfaces.
- DownloadPilot.Infrastructure: implementaties voor watcher, analyse, regels, SQLite, bestandsacties en undo.
- DownloadPilot.Tests: unit- en integratietests met tijdelijke mappen.

## Belangrijkste services (MVP)
- FolderWatchService: bewaakt een of meerdere mappen met FileSystemWatcher.
- FileStabilityService: wacht tot downloads stabiel en niet vergrendeld zijn.
- FileAnalysisService: bepaalt categorie + voorgestelde naam/map.
- ClassificationService: extensie- en trefwoordgebaseerde classificatie.
- RuleEngine: past eenvoudige regels met prioriteit toe.
- FileOperationService: veilige move/rename zonder overschrijven.
- HistoryService: SQLite-registratie van acties.
- UndoService: draait de laatste of een geselecteerde herstelbare verplaatsactie terug.
- DuplicateDetectionService: controleert SHA-256-hashes in geheugen en lokale geschiedenis.
- StartupRegistrationService: beheert optioneel opstarten met Windows via HKCU Run.
- ManualScanService (App): handmatige opruimscan.
- ProposalNotificationService + TrayService (App): bundelt voorstelmeldingen in het systeemvak.

## Datastroom
1. FolderWatchService detecteert nieuw bestand.
2. FileStabilityService valideert dat de download klaar is.
3. FileAnalysisService + ClassificationService maken voorstel.
4. RuleEngine controleert regels.
5. Bij handmatige modus: voorstel naar UI.
6. Bij actie: FileOperationService verplaatst/hernoemt.
7. HistoryService registreert resultaat met hash.
8. DuplicateDetectionService kan latere bestanden met dezelfde hash markeren.
9. UndoService kan de laatste of een door de gebruiker geselecteerde succesvolle actie terugdraaien.

## Veiligheidsprincipes
- Nooit automatisch verwijderen.
- Nooit bestaande bestanden overschrijven.
- Altijd unieke doelnaam.
- Undo-pad via lokale geschiedenis.
- Path traversal geblokkeerd bij ZIP-extractie.
- Alle data lokaal in SQLite.
- Instellingen kunnen met DPAPI worden beschermd.
- Databasebackups blijven lokaal in de DownloadPilot data-map.
