Nachfolgend ein technisches Konzept für ein MVP-SaaS für Handwerksbetriebe mit:
Name Structra
Blazor Server

MudBlazor

Entity Framework Core

PostgreSQL

Docker Deployment auf Railway

Ziel: mobile Baustellen-Dokumentation + Arbeitsberichte.

Das ist bewusst ein kleines, verkaufbares MVP.

1. Ziel des Produkts

SaaS für Handwerksbetriebe zur Dokumentation von Baustellen.

Primäre Probleme:

Fotos werden über WhatsApp verteilt

Arbeitsberichte auf Papier

Projektinformationen verstreut

Das Tool zentralisiert:

Baustellen

Fotos

Arbeitsberichte (Rapporte)

Mitarbeiterplanung (optional später)

2. Kernfunktionen MVP
1 Baustellenverwaltung

Firma legt Projekte an.

Felder:

Baustelle
- Id
- TenantId
- Name
- Kunde
- Adresse
- StartDatum
- EndDatum
- Status
- Beschreibung

Funktionen:

Baustelle erstellen

Baustelle bearbeiten

Liste aller Baustellen

Baustelle archivieren

2 Fotodokumentation

Mitarbeiter fotografieren Fortschritt.

Photo
- Id
- TenantId
- BaustelleId
- UploadedByUserId
- FilePath
- Beschreibung
- CreatedAt
- Latitude
- Longitude

Features:

Foto hochladen

Beschreibung hinzufügen

Galerie anzeigen

Download

PDF Bericht erzeugen

3 Rapportzettel (Arbeitsberichte)

Arbeitszeit + Tätigkeit.

WorkReport
- Id
- TenantId
- BaustelleId
- UserId
- Datum
- Stunden
- Tätigkeit
- Material
- CreatedAt

Beispiel:

12.03.2026
Mitarbeiter: Max

8 Stunden
Installation Steckdosen
Material: Kabel NYM 3x1.5
4 Mitarbeiterverwaltung
User
- Id
- TenantId
- Name
- Email
- PasswortHash
- Role

Rollen:

Admin
Büro
Mitarbeiter
3 Multitenancy

Jede Firma ist ein Tenant.

Tenant
- Id
- Name
- CreatedAt
- Plan

Alle Tabellen enthalten:

TenantId

EF Core Filter:

builder.Entity<BaseEntity>()
    .HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);

Tenant wird z. B. ermittelt über:

Subdomain

oder TenantId im Login

4 Datenbankstruktur

Minimalstruktur.

Tenant
User
Baustelle
Photo
WorkReport

Beziehungen:

Tenant
 ├── Users
 ├── Baustellen
 │      ├── Photos
 │      └── WorkReports
5 Backend Architektur

Projektstruktur:

src/

Server
Application
Domain
Infrastructure
Shared

Domain:

Entities
ValueObjects
Enums

Application:

Services
DTOs
Interfaces

Infrastructure:

DbContext
Repositories
Storage
6 File Storage

Fotos nicht in DB speichern.

Optionen:

1 Local Storage
2 Object Storage

Für MVP:

/uploads/{tenant}/{baustelle}/{photo}.jpg

Später:

S3

Cloudflare R2

7 Responsive UI Konzept

Mit MudBlazor.

Layout:

Desktop

Sidebar
Topbar
Content

Mobile

Topbar
Hamburger Menu
Content
Bottom Actions
Baustellenliste

Desktop

Table
Name | Kunde | Status | Aktionen

Mobile

Card Layout

Baustelle
Adresse
Status
[Details]

MudBlazor:

MudTable
MudCard
MudGrid
MudHidden
Baustellen Detailseite

Tabs:

Übersicht
Fotos
Rapporte

Beispiel:

MudTabs
  TabPanel Übersicht
  TabPanel Fotos
  TabPanel Rapporte
8 Foto Upload (Handy)

Wichtig für Handwerker.

Input:

InputFile
accept="image/*"
capture="environment"

Das öffnet direkt die Kamera.

9 Beispiel UI Flow

Mitarbeiter Workflow.

1 Login
2 Baustelle öffnen
3 Foto machen
4 Kommentar schreiben
5 Speichern

Dauer:

~10 Sekunden.

10 Authentifizierung

Optionen:

1 eigenes Login
2 Identity

Einfach:

ASP.NET Identity

Mit:

Cookie Auth

Rollen

11 PDF Bericht

Generieren aus:

Fotos

Arbeitsberichten

Libraries:

QuestPDF

DinkToPdf

Beispiel:

Baustelle
Datum
Fotos
Arbeitsberichte

Export:

Baustellenbericht.pdf
12 Deployment Architektur

Docker Container.

Blazor Server
PostgreSQL

Auf Railway.

Container:

app
postgres
Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish .
ENTRYPOINT ["dotnet", "App.dll"]
13 EF Core Setup

NuGet:

Npgsql.EntityFrameworkCore.PostgreSQL

Connection:

Host
Port
Database
User
Password
14 Preisstrategie

Einfach.

Starter
1 Betrieb
10 Mitarbeiter
19 €/Monat

Später:

Pro
unlimited
39 €

Payments:

Stripe

15 Roadmap

Phase 1 (MVP)

Login

Baustellen

Foto Upload

Arbeitsberichte

Phase 2

Material

Planung

PDF Export

Phase 3

Rechnungsintegration

API

Mobile PWA