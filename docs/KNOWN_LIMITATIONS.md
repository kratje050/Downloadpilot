# Bekende Beperkingen (huidige MVP)

- OCR werkt optioneel lokaal via Tesseract voor afbeeldingsbestanden, maar vereist handmatig aanwezige taaldata in `%LocalAppData%/DownloadPilot/tessdata`.
- OCR op gescande PDF-pagina's (eerst renderen naar afbeelding) is nog niet toegevoegd.
- Duplicaatdetectie is exact op SHA-256 en gebruikt de lokale geschiedenis; perceptual hashing voor vergelijkbare afbeeldingen is nog niet toegevoegd.
- Geavanceerde regelbouwer (meerdere voorwaarden/acties) is nog niet compleet.
- Automatische modus heeft nog geen uitgebreide UI-validatie per regel.
- Installer bevat Inno Setup script, maar vereist lokale Inno Setup installatie om te bouwen.
- Portable build wordt via script gemaakt; de eerste-startwizard draait in zowel de portable versie als de geïnstalleerde app.
- Nederlandse documentherkenning gebruikt momenteel alleen eenvoudige trefwoorden.
