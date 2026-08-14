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

Typowy serwis zawiera następujące projekty:

Domain
  Czysta logika biznesowa: agregaty, encje, obiekty wartości, reguły, niezmienniki
  oraz zdarzenia domenowe. Warstwa nie zależy od bazy danych, HTTP ani Kafka.

Application
  Przypadki użycia implementowane jako komendy i zapytania CQRS. Zawiera handlery
  MediatR, walidację FluentValidation, kontrakty oraz mapowania modeli aplikacyjnych.

Persistence
  Dostęp do PostgreSQL przez Entity Framework Core, DbContext, mapowania encji,
  repozytoria, Unit of Work, migracje, projekcje odczytowe i tabelę Outbox.

Infrastructure
  Integracje techniczne, między innymi Kafka, tokeny, haszowanie haseł, SMTP,
  Redis oraz klienci HTTP do innych serwisów.

API
  Kontrolery ASP.NET Core, uwierzytelnianie, autoryzacja, kontrakty HTTP,
  mapowanie odpowiedzi, OpenAPI i konfiguracja potoku żądań.

Workers
  Osobne procesy robocze. Projekt Consumers odbiera zdarzenia z Kafka, natomiast
  OutboxPublisher publikuje do Kafka zdarzenia zapisane wcześniej w bazie serwisu.

GatewayService jest świadomym wyjątkiem: jako warstwa proxy i agregacji zawiera tylko
projekty API i Infrastructure. Nie posiada własnej domeny ani bazy danych.


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

Klient webowy aplikacji. Odpowiada za interfejs użytkownika, routing w przeglądarce,
wywołania API, lokalny stan UI, pamięć podręczną danych serwerowych oraz połączenie
SignalR. Korzysta wyłącznie z publicznego wejścia udostępnianego przez GatewayService.


2.2. GatewayService
-------------------

Jedyny publiczny punkt wejścia do backendu. Odpowiada za:

- reverse proxy i routing żądań do właściwego mikroserwisu przy użyciu YARP,
- weryfikację tokenów JWT i polityk dostępu na chronionych trasach,
- konfigurację CORS dla klienta webowego,
- przekazywanie połączeń i negocjacji SignalR do RealtimeService,
- agregowanie danych z kilku serwisów przez dedykowane fasady,
- udostępnianie katalogu tras i informacji o stanie bramy.

Gateway nie implementuje logiki domenowej. Proste trasy są przekazywane do serwisów,
a operacje wymagające połączenia kilku źródeł korzystają z fasad orkiestracyjnych.


2.3. AuthService
----------------

Właściciel tożsamości i poświadczeń użytkownika. Odpowiada za:

- rejestrację konta,
- logowanie i wylogowanie,
- wydawanie i odświeżanie tokenów,
- obsługę access tokenów i refresh tokenów przez OpenIddict,
- bezpieczne haszowanie i weryfikację haseł algorytmem Argon2id,
- przechowywanie kont oraz danych autoryzacyjnych,
- publikowanie zdarzeń dotyczących cyklu życia tożsamości,
- synchronizację zmiany i potwierdzenia adresu logowania.

AuthService nie jest właścicielem rozbudowanego profilu użytkownika. Po utworzeniu konta
publikuje zdarzenie, na podstawie którego UserProfileService tworzy swoją część danych.


2.4. UserProfileService
-----------------------

Właściciel profilu użytkownika i jego danych kontaktowych. Odpowiada za:

- utworzenie profilu na podstawie zdarzenia z AuthService,
- odczyt, wyszukiwanie, aktualizację i usuwanie profilu,
- nazwę użytkownika oraz dane prezentowane innym użytkownikom,
- wiele adresów e-mail i numerów telefonu,
- wybór głównego adresu e-mail i głównego numeru telefonu,
- powiązanie adresu uwierzytelniającego z AuthService,
- proces wysyłki i potwierdzania linku weryfikacyjnego e-mail,
- publikowanie projekcji profilu używanych przez inne serwisy.

Żądanie wysłania wiadomości weryfikacyjnej jest publikowane asynchronicznie i obsługiwane
przez NotificationService.


2.5. ChatService
----------------

Główny obszar rozmów i wiadomości. Odpowiada za:

- tworzenie rozmów prywatnych typu Duet,
- tworzenie i odczytywanie rozmów grupowych,
- kopiowanie rozmowy Duet jako grupy,
- dodawanie oraz usuwanie uczestników grup,
- wysyłanie, pobieranie i oznaczanie wiadomości jako dostarczone,
- oznaczanie rozmowy jako przeczytanej,
- indywidualny stan uczestnika: blokowanie, wyciszenie i ukrycie rozmowy,
- listę kontaktów użytkownika,
- lokalną projekcję podstawowych danych profili uczestników,
- publikowanie zdarzeń o wiadomościach, rozmowach i członkostwie.

Relacja kontaktu jest modelowana przez rozmowę Duet. Oznacza to, że ChatService jest
właścicielem zarówno komunikacji prywatnej, jak i wynikającej z niej relacji kontaktowej.


2.6. PresenceService
--------------------

Właściciel informacji o obecności użytkownika. Odpowiada za:

- inicjalizowanie, odświeżanie i usuwanie stanu obecności,
- ręczną zmianę statusu i zapis preferencji użytkownika,
- udostępnianie statusów pojedynczo i zbiorczo,
- przechowywanie szybko zmieniającego się stanu w Redis,
- utrzymywanie projekcji obserwatorów wynikającej z kontaktów,
- kierowanie zmiany statusu tylko do zainteresowanych kontaktów,
- publikowanie zmian obecności do RealtimeService.

Rozdzielenie obecności od profilu i rozmów pozwala niezależnie skalować często
aktualizowany, krótkotrwały stan użytkowników.


2.7. RealtimeService
--------------------

Warstwa dostarczania zdarzeń online. Odpowiada za:

- autoryzowane połączenia SignalR,
- rejestrację i wyrejestrowanie połączeń użytkownika,
- przechowywanie rozproszonego rejestru połączeń w Redis,
- przypisywanie połączeń do grup odpowiadających rozmowom,
- odbieranie z Kafka zdarzeń o wiadomościach, rozmowach i obecności,
- kierowanie zdarzeń do właściwych użytkowników i urządzeń,
- synchronizację grup po dodaniu lub usunięciu uczestników rozmowy.

Serwis może obsługiwać wiele równoległych instancji API, ponieważ współdzielony stan
połączeń znajduje się poza pamięcią pojedynczego procesu.


2.8. NotificationService
------------------------

Właściciel procesu wysyłania i rejestrowania powiadomień e-mail/SMS. Aktualna
implementacja koncentruje się na wiadomościach weryfikacyjnych e-mail i odpowiada za:

- odbieranie żądań powiadomień z Kafka,
- tworzenie rekordu powiadomienia i śledzenie jego statusu,
- wysyłkę wiadomości przez SMTP przy użyciu MailKit,
- zapis powodzenia albo błędu dostarczenia,
- udostępnianie wewnętrznego podglądu ostatnich powiadomień.

W środowisku lokalnym rolę serwera SMTP i skrzynki podglądowej pełni MailHog.


2.9. HarnessService
-------------------

Serwis pomocniczy przeznaczony wyłącznie do developmentu i testów akceptacyjnych
infrastruktury przekrojowej. Nie jest funkcjonalnością produktu. Służy do weryfikacji:

- potoku projekcji,
- poprawnego przetwarzania wiadomości Kafka,
- wielopoziomowych retry,
- izolowania błędnych wiadomości w DLQ,
- zachowania wspólnych komponentów bez obciążania testami domen produkcyjnych.


2.10. Common
------------

Zestaw współdzielonych bibliotek wykorzystywanych przez wszystkie serwisy:

- FlowChat.Core — podstawowe kontrakty, markery i prymitywy,
- FlowChat.Shared.Domain — bazowe elementy DDD, typed IDs i zdarzenia domenowe,
- FlowChat.Shared.Application — CQRS, wyniki, walidacja i wspólne procesory,
- FlowChat.Shared.Persistence — EF Core, repozytoria, Unit of Work i Outbox,
- FlowChat.Shared.Infrastructure — konfiguracja, Kafka, telemetria i integracje,
- FlowChat.Shared.API — wspólna konfiguracja API, JWT, OpenAPI i obsługa błędów,
- FlowChat.Shared.Consumers — wspólny potok konsumentów Kafka, retry i DLQ.

Common udostępnia mechanizmy techniczne, lecz nie powinien przejmować logiki biznesowej
należącej do konkretnego mikroserwisu.


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


7. TESTOWANIE
=============

Pełne rozwiązanie backendowe:

  dotnet test FlowChat.slnx

Przykład uruchomienia testów jednego serwisu:

  dotnet test ChatService/FlowChat.ChatService.slnx

Frontend:

  cd ReactClient
  npm run test
  npx tsc --noEmit
  npm run build

Testy jednostkowe sprawdzają logikę domenową, handlery i izolowane integracje. Testy
integracyjne weryfikują współpracę warstw, bazę danych, rejestrację zależności i potok
HTTP. Testy AAT oraz Testcontainers są używane tam, gdzie wymagane jest rzeczywiste
zachowanie Kafka, PostgreSQL albo Redis.


8. PODSUMOWANIE
===============

FlowChat rozdziela odpowiedzialności biznesowe między niezależne mikroserwisy i łączy
spójność transakcyjną lokalnej bazy z asynchroniczną komunikacją Kafka. GatewayService
zapewnia jedno wejście HTTP, PostgreSQL przechowuje trwały stan każdego serwisu, Redis
obsługuje szybki stan obecności i połączeń, a RealtimeService dostarcza zmiany przez
SignalR. Wspólne biblioteki ujednolicają mechanizmy techniczne, zachowując logikę
biznesową we właściwych granicach domenowych.
