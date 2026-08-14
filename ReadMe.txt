FLOWCHAT
========

FlowChat jest rozwijaną w architekturze mikroserwisowej aplikacją komunikatora internetowego.
System obsługuje rejestrację i uwierzytelnianie użytkowników, profile, rozmowy prywatne
i grupowe, wiadomości, kontakty, statusy obecności, powiadomienia oraz komunikację
w czasie rzeczywistym. Backend jest zbudowany na platformie .NET 10, a interfejs
użytkownika stanowi aplikacja React uruchamiana w przeglądarce.

Repozytorium jest monorepo: zawiera wszystkie serwisy backendowe, współdzielone biblioteki,
klienta webowego, testy oraz skrypty uruchamiające lokalną infrastrukturę.


1. ARCHITEKTURA SYSTEMU
======================

FlowChat łączy kilka uzupełniających się stylów architektonicznych:

- mikroserwisy dzielą system na niezależne obszary biznesowe,
- Clean Architecture rozdziela logikę domenową od transportu i infrastruktury,
- Domain-Driven Design (DDD) modeluje reguły biznesowe za pomocą agregatów, encji,
  obiektów wartości i zdarzeń domenowych,
- CQRS rozdziela operacje zmieniające stan (commands) od odczytów (queries),
- vertical slices grupują kod według funkcjonalności, a nie wyłącznie według typu klasy,
- komunikacja synchroniczna odbywa się przez REST API,
- komunikacja asynchroniczna odbywa się przez zdarzenia publikowane w Apache Kafka,
- SignalR dostarcza zdarzenia do połączonych klientów w czasie rzeczywistym.

Uproszczony przepływ komunikacji:

  +-------------------+
  | ReactClient       |
  | przeglądarka      |
  +---------+---------+
            |
            | HTTPS / REST / SignalR
            v
  +---------+---------+
  | GatewayService    |
  | YARP + agregacja  |
  +---------+---------+
            |
            | synchroniczne wywołania HTTP
            v
  +---------+-----------------------------------------------------------+
  | Auth | Chat | UserProfile | Presence | Realtime | Notification     |
  +---------+-----------------------------------------------------------+
            |                         |
            | EF Core                 | Outbox Publisher / Consumers
            v                         v
  +---------+---------+      +--------+---------+
  | PostgreSQL        |      | Apache Kafka     |
  | baza per serwis   |      | zdarzenia        |
  +-------------------+      +--------+---------+
                                      |
                                      v
                             +--------+---------+
                             | RealtimeService  |
                             | SignalR + Redis  |
                             +--------+---------+
                                      |
                                      v
                              połączeni klienci


1.1. Granice mikroserwisów i własność danych
---------------------------------------------

Każdy serwis jest właścicielem własnego modelu domenowego i własnej bazy PostgreSQL.
Serwisy nie odczytują bezpośrednio tabel należących do innych serwisów. Dane potrzebne
w innym obszarze są pobierane przez API albo replikowane w postaci lokalnych projekcji
na podstawie zdarzeń z Kafka. Dzięki temu wdrożenie i rozwój jednego serwisu nie wymaga
współdzielenia jego schematu bazy danych z pozostałymi serwisami.

Model zapisu korzysta z agregatów domenowych. Model odczytu używa prostych encji
persystencyjnych i DTO, dzięki czemu zapytania nie muszą odtwarzać całego agregatu.


1.2. Warstwy Clean Architecture
-------------------------------

Serwisy biznesowe są podzielone na projekty Domain, Application, Persistence,
Infrastructure i API. Procesy Kafka działają jako osobne hosty Workers: Consumers
odbiera zdarzenia, a OutboxPublisher publikuje zdarzenia zapisane w bazie serwisu.

GatewayService jest świadomym wyjątkiem — jako warstwa proxy i agregacji zawiera tylko
projekty API i Infrastructure oraz nie posiada własnej domeny ani bazy danych.


1.3. CQRS, DDD i obsługa wyniku
------------------------------

Kontrolery przekazują żądania do MediatR i nie wykonują bezpośrednio operacji na
repozytoriach. Walidacja wejścia znajduje się w warstwie Application. Reguły biznesowe
i niezmienniki są egzekwowane w modelu domenowym, a zmiany stanu agregatu mogą generować
zdarzenia domenowe.

Handlery zwracają FlowChatResult<T>, który jawnie reprezentuje sukces albo błąd.
Pozwala to mapować błędy aplikacyjne na przewidywalne odpowiedzi HTTP bez używania
wyjątków do sterowania typowym przepływem biznesowym.


1.4. Komunikacja asynchroniczna, Outbox i odporność na błędy
------------------------------------------------------------

Zmiana biznesowa i odpowiadający jej wpis Outbox są zapisywane w jednej transakcji
bazy danych. Osobny OutboxPublisher odczytuje oczekujące wpisy i publikuje je do Kafka.
Zapobiega to sytuacji, w której dane zostały zapisane, ale zdarzenie utracono wskutek
chwilowej awarii brokera.

Consumers przetwarzają zdarzenia integracyjne niezależnie od procesu API. Obsługa
wiadomości przewiduje wielopoziomowe ponowienia z rosnącym opóźnieniem. Zdarzenie,
którego nie udało się obsłużyć, trafia do osobnego dead-letter topic (DLQ), co izoluje
błędną wiadomość od prawidłowego ruchu.

System rozróżnia:

- zdarzenia integracyjne opisujące zdarzenia biznesowe,
- zdarzenia projekcyjne synchronizujące techniczne modele odczytowe między serwisami.

Tematy projekcyjne mają przyrostek „-projection”, a tematy biznesowe pozostają od nich
oddzielone. Ułatwia to obserwowanie przepływu i analizę problemów w Kafka.


1.4.1. Przepływ projekcji między serwisami
------------------------------------------

Produkcyjny konsument przetwarza jedno zdarzenie projekcyjne jako jedną transakcyjną
komendę ProjectionSingle. Przepływ wygląda następująco:

1. Serwis źródłowy publikuje ProjectionIntegrationEvent<TReadModel> przez swój
   transakcyjny Outbox.
2. Dedykowany subscriber serwisu docelowego sprawdza wersję agregatu i payload,
   opcjonalnie odrzuca zdarzenia nieistotne dla lokalnej projekcji, mapuje transportowy
   event na ProjectionSingleCommand i przekazuje komendę do MediatR.
3. TransactionalCommandHandlerBase wykonuje UPSERT uwzględniający wersję danych oraz
   zapisuje kliencki offset Kafka w tej samej transakcji EF Core.
4. Starsze lub powtórzone zdarzenie nie może nadpisać nowszego wiersza projekcji.
5. Tabele projekcyjne odzwierciedlają dane serwisu źródłowego bez wprowadzania przez
   konsumenta dodatkowych reguł lub mutacji biznesowych.

Subscriber odpowiada za walidację i mapowanie kontraktu transportowego, natomiast
repozytorium Persistence jest właścicielem zależnej od bazy implementacji UPSERT.
Repozytoria zapisu nadal operują na agregatach domenowych, a repozytoria odczytu na
prostych encjach persystencyjnych.

Przeniesienie wiadomości pomiędzy poziomami retry zapisuje wiadomość wychodzącą i offset
źródłowy atomowo przez Silverback EF Outbox. Każdy poziom korzysta z osobnego tematu oraz
metadanych RetryAtUtc. Konsumenci nie używają retry „w miejscu”, ponieważ byłoby ono
niezgodne z cyklem życia zakresu klienckiego magazynu offsetów Kafka.

Mechanizm ProjectionBulk pozostaje wyłącznie w HarnessService jako testowy, transakcyjny
batch UPSERT. Deduplikuje wartości według klucza, nie posiada własnego konsumenta retry,
producenta retry ani ścieżki DLQ. Nieudany batch wycofuje transakcję i pozostawia offset
wiadomości źródłowej niezatwierdzony.


1.5. Komunikacja w czasie rzeczywistym
--------------------------------------

RealtimeService wystawia autoryzowany hub SignalR pod adresem /hubs/chat. Rejestruje
aktywne połączenia użytkowników w Redis, zarządza grupami SignalR i przekazuje do
klientów informacje o nowych wiadomościach, zmianach rozmów oraz statusach obecności.

Zdarzenia pochodzące z ChatService i PresenceService są odbierane przez proces
RealtimeService.Consumers. Dzięki temu serwisy biznesowe nie muszą znać konkretnych
połączeń przeglądarek ani bezpośrednio komunikować się z klientami.


2. ODPOWIEDZIALNOŚCI KOMPONENTÓW
===============================

2.1. ReactClient
----------------

Klient webowy zbudowany w React i TypeScript. Udostępnia interfejs użytkownika,
komunikuje się z backendem przez GatewayService i odbiera zdarzenia SignalR.


2.2. GatewayService
-------------------

Publiczny punkt wejścia do backendu. Przez YARP kieruje żądania do właściwych serwisów,
weryfikuje dostęp i agreguje odpowiedzi wymagające danych z kilku źródeł.


2.3. AuthService
----------------

Zarządza kontami, logowaniem, wylogowaniem, hasłami oraz tokenami dostępu i odświeżania.
Publikuje zdarzenia dotyczące utworzenia i zmian tożsamości użytkownika.


2.4. UserProfileService
-----------------------

Zarządza profilami użytkowników, ich nazwami, adresami e-mail i numerami telefonu.
Obsługuje wyszukiwanie profili, weryfikację adresów e-mail oraz publikowanie danych
profilowych potrzebnych innym serwisom.


2.5. ChatService
----------------

Zarządza rozmowami prywatnymi i grupowymi, wiadomościami, uczestnikami oraz stanem
przeczytania. Jest także właścicielem kontaktów, które są modelowane jako rozmowy Duet,
oraz ustawień uczestnika takich jak blokowanie, wyciszenie i ukrycie rozmowy.


2.6. PresenceService
--------------------

Zarządza bieżącymi statusami obecności i preferencjami użytkowników. Przechowuje szybki
stan w Redis i przekazuje zmiany obecności do zainteresowanych kontaktów.


2.7. RealtimeService
--------------------

Utrzymuje połączenia SignalR i dostarcza klientom zdarzenia o wiadomościach, rozmowach
oraz obecności. Rejestr połączeń przechowuje w Redis, dzięki czemu może działać w wielu
instancjach.


2.8. NotificationService
------------------------

Odbiera z Kafka żądania wysyłki i rejestruje status powiadomień. Aktualna implementacja
wysyła przez SMTP wiadomości e-mail służące do weryfikacji adresu użytkownika.


2.9. HarnessService
-------------------

Pomocniczy serwis developerski do testowania wspólnej infrastruktury, między innymi
projekcji, przetwarzania Kafka, wielopoziomowych retry i DLQ. Nie stanowi funkcjonalności
produktu.


2.10. Common
------------

Zestaw bibliotek współdzielących podstawowe elementy DDD, CQRS, persystencji, API,
integracji i obsługi konsumentów. Zawiera mechanizmy techniczne wspólne dla serwisów,
ale nie ich logikę biznesową.


3. STACK TECHNOLOGICZNY
=======================

Backend i API
  - C# 13
  - .NET 10 / ASP.NET Core 10
  - kontrolery Web API oraz osobne hosty Worker
  - SignalR
  - YARP Reverse Proxy
  - OpenAPI / Swagger

Architektura aplikacyjna
  - Clean Architecture
  - Domain-Driven Design
  - CQRS i MediatR
  - FluentValidation
  - AutoMapper
  - FlowChatResult<T> / CSharpFunctionalExtensions

Dane
  - PostgreSQL 16
  - Entity Framework Core 10
  - Npgsql
  - Redis 7 i StackExchange.Redis
  - osobna baza danych dla każdego serwisu biznesowego

Messaging i integracja
  - Apache Kafka
  - Silverback
  - Confluent.Kafka
  - Transactional Outbox
  - retry topics i dead-letter topics
  - REST/HTTPS między bramą i serwisami

Bezpieczeństwo
  - OpenIddict
  - JWT Bearer
  - access token i refresh token
  - Argon2id do haszowania haseł
  - polityki autoryzacji w GatewayService
  - klucze API dla wewnętrznej komunikacji serwis-serwis
  - Infisical do lokalnego zarządzania sekretami

Frontend
  - React 19
  - TypeScript 5
  - Vite 7
  - React Router
  - TanStack Query
  - Zustand
  - Axios
  - Microsoft SignalR Client
  - React Virtuoso
  - Vitest

Powiadomienia
  - MailKit / SMTP
  - MailHog w środowisku developerskim

Obserwowalność
  - OpenTelemetry i eksport OTLP
  - Grafana Alloy
  - Prometheus — metryki
  - Loki — logi
  - Tempo — ślady rozproszone
  - Grafana — wizualizacja

Testy
  - xUnit
  - FluentAssertions
  - Moq i AutoFixture
  - WebApplicationFactory
  - SQLite/InMemory dla testów integracyjnych
  - Testcontainers dla PostgreSQL, Kafka i Redis
  - testy jednostkowe, integracyjne oraz AAT

Infrastruktura lokalna
  - Docker / Docker Compose
  - skrypty PowerShell do uruchamiania i resetowania środowiska
  - Kafka UI i RedisInsight do inspekcji infrastruktury


4. NAJWAŻNIEJSZE PRZEPŁYWY BIZNESOWE
====================================

4.1. Rejestracja użytkownika
----------------------------

1. ReactClient wysyła żądanie rejestracji przez GatewayService.
2. AuthService tworzy konto, haszuje hasło i zapisuje dane wraz z wpisem Outbox.
3. OutboxPublisher publikuje zdarzenie utworzenia użytkownika do Kafka.
4. UserProfileService.Consumers tworzy profil użytkownika w swojej bazie.
5. UserProfileService publikuje projekcję profilu dla zainteresowanych serwisów.
6. Żądanie weryfikacji adresu e-mail trafia przez Kafka do NotificationService.
7. NotificationService wysyła wiadomość SMTP z linkiem potwierdzającym.


4.2. Wysłanie wiadomości
------------------------

1. Uwierzytelniony klient wysyła wiadomość przez GatewayService do ChatService.
2. ChatService sprawdza reguły rozmowy, zapisuje wiadomość oraz zdarzenie Outbox.
3. OutboxPublisher publikuje zdarzenie wiadomości do Kafka.
4. RealtimeService.Consumers odbiera zdarzenie i kieruje je do RealtimeService API.
5. RealtimeService ustala aktywne połączenia odbiorców i wysyła komunikat przez SignalR.
6. Klienci aktualizują widok rozmowy bez odpytywania API o każdą zmianę.


4.3. Zmiana statusu obecności
-----------------------------

1. PresenceService aktualizuje bieżący status i preferencje użytkownika.
2. Na podstawie projekcji kontaktów ustala użytkowników obserwujących zmianę.
3. Zdarzenie obecności jest publikowane i odbierane przez RealtimeService.
4. SignalR dostarcza zmianę tylko do odpowiednich połączonych klientów.


5. STRUKTURA REPOZYTORIUM
========================

FlowChat.slnx
  Globalne rozwiązanie .NET zawierające oficjalne projekty backendowe i testowe.

FlowChat.code-workspace
  Główny workspace VS Code z konfiguracjami uruchomieniowymi dla API, workerów,
  klienta React oraz compoundami „All APIs”, „All Workers” i „All Services”.

{Service}/FlowChat.{Service}.slnx
  Lokalne rozwiązanie konkretnego serwisu, wygodne podczas pracy nad jednym obszarem.

{Service}/src
  Kod produkcyjny warstw serwisu oraz projekty Workers.

{Service}/tests
  Testy jednostkowe, integracyjne i — tam, gdzie są potrzebne — AAT.

Common/src i Common/tests
  Wspólne biblioteki oraz ich testy.

ReactClient
  Aplikacja React + TypeScript.

Scripts
  Docker Compose i skrypty PowerShell dla PostgreSQL, Kafka, Redis, MailHog,
  Infisical oraz stosu obserwowalności.


6. KONFIGURACJA I URUCHOMIENIE LOKALNE
======================================

Wymagania:

- .NET 10 SDK,
- Node.js 20 lub nowszy,
- Docker Desktop lub zgodne środowisko Docker,
- PowerShell,
- opcjonalnie VS Code z otwartym FlowChat.code-workspace.

Skrypty bootstrap znajdują się w katalogach Scripts/PostgreSQL, Scripts/Kafka,
Scripts/Redis, Scripts/MailHog, Scripts/Infisical i Scripts/Observability. Można uruchomić
tylko potrzebne elementy albo całe przygotowane stosy, na przykład Kafka z Kafka UI,
Redis z RedisInsight oraz kompletny stack obserwowalności.

Po uruchomieniu PostgreSQL migracje wszystkich zarejestrowanych serwisów wykonuje skrypt:

  ./Scripts/PostgreSQL/migrate-all.ps1

Backend można uruchamiać osobno z lokalnych rozwiązań serwisów albo za pomocą compoundów
w FlowChat.code-workspace. Domyślnym publicznym adresem bramy w środowisku developerskim
jest https://localhost:7270.

Uruchomienie klienta:

  cd ReactClient
  npm install
  npm run dev

Klient działa domyślnie pod adresem http://localhost:5173 i oczekuje GatewayService pod
adresem https://localhost:7270. Adresy można zmienić przez zmienne VITE_GATEWAY_API_URL
i VITE_REALTIME_API_URL.

Konfiguracja serwisów znajduje się w appsettings.json i appsettings.Development.json.
Sekrety oraz wartości lokalne nie powinny trafiać do repozytorium; mogą być dostarczane
przez zmienne środowiskowe, lokalne pliki konfiguracyjne albo Infisical.