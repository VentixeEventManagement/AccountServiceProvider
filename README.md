# AccountServiceProvider

AccountServiceProvider är en mikrotjänst byggd i .NET som hanterar användarkonton och kommunicerar med AuthServiceProvider via gRPC. Tjänsten lagrar och hämtar data från en SQL-databas i Azure.

## Innehåll

- [Beskrivning](#beskrivning)
- [Teknikstack](#teknikstack)
- [Projektstruktur](#projektstruktur)
- [Installation](#installation)
- [Exempel på gRPC-metoder](#exempel-på-grpc-metoder)
- [Databas](#databas)
- [Kontakt](#kontakt)

## Beskrivning

Mikrotjänsten erbjuder funktioner för att:
- Skapa konton
- Validera inloggningsuppgifter
- Uppdatera användaruppgifter
- Hantera e-postbekräftelser och lösenordsåterställning
- Byta användarroll
- Hämta konton eller specifik användarinformation

All autentisering sker via AuthServiceProvider.

## Teknikstack

- .NET 9
- ASP.NET Core gRPC
- Entity Framework Core
- SQL-databas (Azure)
- C#
- Dependency Injection
- Clean Architecture-liknande struktur med endast Presentation-lager

## Projektstruktur

```text
AccountServiceProvider/
│
├── Protos/                  # gRPC-protokolldefinitioner (.proto)
├── Models/                  # Domänmodeller (t.ex. Account, Role)
├── Services/                # Tjänstelager med affärslogik
├── Controllers/             # gRPC-tjänstklasser
├── Seeders/                 # Används för att skapa standardroller 
├── Program.cs               # Konfiguration, DI och startup
├── appsettings.json         # Konfiguration (t.ex. connection string)
└── AccountServiceProvider.csproj
```

# Aktivitetsdiagram
![AccountServiceProvider_Aktivitetsdiagram](https://github.com/user-attachments/assets/6e8f0be7-28ff-4356-ba40-bd3907db71f8)

# Sekvensdiagram

![AccountServiceProvider_Sekvensdiagram](https://github.com/user-attachments/assets/d5cba61a-e633-4187-8de8-a451cdac5545)
