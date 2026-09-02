# ReadyForge

Orodje za pripravo namiznih racunalnikov z Windows 10 ali Windows 11.

Razlicica 2.4 ob napaki WinGet `0x8A150011` za Chrome samodejno uporabi uradni Google Enterprise MSI in preveri dejansko namestitev.

## Prenos

[Prenesi najnovejsi ReadyForge.exe](https://github.com/Ruzzix005/klasika-pc-setup/releases/latest/download/ReadyForge.exe)

Program zahteva skrbniske pravice. Ker EXE ni digitalno podpisan s placljivim code-signing certifikatom, lahko Microsoft SmartScreen prikaze opozorilo.

## Funkcije

- namestitev ali posodobitev Google Chrome,
- namestitev ali posodobitev 7-Zip,
- namestitev ali posodobitev Adobe Acrobat Reader 64-bit,
- nacrt `ReadyForge - visoka ucinkovitost`,
- izklop spanja, hibernacije, ugašanja zaslona in diska,
- izklop hitrega zagona (Fast Startup),
- izklop USB selective suspend in PCIe Link State Power Management,
- izklop varcevanja na USB in aktivnih fizicnih mreznih karticah,
- dnevnik v `C:\ProgramData\ReadyForge\Logs`.
- pregled istega seznama kot `Nadzorna plosca > Programi in funkcije`,
- rocna izbira programov za odstranitev z opozorili za gonilnike in OEM updaterje.
- predhodni pregled modela, Windows, RAM-a, aktivacije, interneta in stanja ponovnega zagona,
- prikaz stanja vsakega opravila in skupnega napredka,
- preverjanje aktivnega nacrta, nastavitev `Nikoli` in izklopa Fast Startup,
- iskanje in namestitev Windows posodobitev,
- pregled naprav z manjkajocimi ali okvarjenimi gonilniki.

## Build

GitHub Actions ob vsaki spremembi veje `main` izdela samostojen Windows x64 EXE in posodobi Release `latest`.
