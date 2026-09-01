# Klasika PC Setup

Orodje za pripravo namiznih racunalnikov z Windows 10 ali Windows 11.

## Prenos

[Prenesi najnovejsi Klasika-PC-Setup.exe](https://github.com/Ruzzix005/klasika-pc-setup/releases/latest/download/Klasika-PC-Setup.exe)

Program zahteva skrbniske pravice. Ker EXE ni digitalno podpisan s placljivim code-signing certifikatom, lahko Microsoft SmartScreen prikaze opozorilo.

## Funkcije

- namestitev ali posodobitev Google Chrome,
- namestitev ali posodobitev 7-Zip,
- namestitev ali posodobitev Adobe Acrobat Reader 64-bit,
- nacrt `Klasika - visoka ucinkovitost`,
- izklop spanja, hibernacije, ugašanja zaslona in diska,
- izklop USB selective suspend in PCIe Link State Power Management,
- izklop varcevanja na USB in aktivnih fizicnih mreznih karticah,
- dnevnik v `C:\ProgramData\KlasikaPCSetup\Logs`.

## Build

GitHub Actions ob vsaki spremembi veje `main` izdela samostojen Windows x64 EXE in posodobi Release `latest`.
