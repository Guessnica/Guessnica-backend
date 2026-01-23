# Instrukcja uruchomienia projektu Guessnica Backend

## Wymagania wstępne
- Docker i Docker Compose
- Git
- .NET 8 SDK (opcjonalnie, do lokalnego developmentu)

## Pierwsze uruchomienie
Należy wykonać kroki 1-4 z poniższej instrukcji. Pierwsze uruchomienie wymaga pełnej konfiguracji środowiska.

---

## Krok 1: Klonowanie repozytorium
```bash
git clone https://github.com/your-username/Guessnica-backend
cd Guessnica-backend
```

---

## Krok 2: Konfiguracja zmiennych środowiskowych

Utwórz plik `.env` w głównym katalogu projektu:

```bash
# .env
POSTGRES_USER=guessnica
POSTGRES_PASSWORD=guessnica
POSTGRES_DB=guessnica
ASPNETCORE_ENVIRONMENT=Development
JWT_KEY=SuperSekretnyKluczJWT1234567890!@#LongSecretKey
FACEBOOK_APP_SECRET=your_facebook_app_secret
EMAIL_PASSWORD=your_gmail_app_password
```

**Uwaga:** Należy wypełnić rzeczywiste wartości dla `FACEBOOK_APP_SECRET` i `EMAIL_PASSWORD`.

---

## Krok 3: Przygotowanie środowiska

1. Upewnij się, że porty **5432** (PostgreSQL), **8082** (Backend API) i **8081** (pgAdmin) są wolne na Twoim komputerze.

2. Uruchom kontenery Docker:
```bash
docker-compose build
docker-compose up -d
```

3. Sprawdź status kontenerów:
```bash
docker-compose ps
```

Powinny być uruchomione 3 kontenery:
- `guessnica-backend-app` - aplikacja ASP.NET Core
- `guessnica-db` - baza danych PostgreSQL
- `guessnica-pgadmin` - interfejs pgAdmin (opcjonalnie)

---

## Krok 4: Inicjalizacja bazy danych

### Metoda 1: Automatyczna inicjalizacja (zalecane)

Użyj przygotowanego skryptu `init.sh`:

```bash
# Nadaj uprawnienia do wykonania
chmod +x init.sh

# Uruchom skrypt
./init.sh
```

### Metoda 2: Ręczna inicjalizacja

Po uruchomieniu kontenerów wykonaj poniższe komendy:

```bash
# Poczekaj aż baza danych się uruchomi (10-15 sekund)
sleep 10

# Wejdź do kontenera aplikacji
docker exec -it guessnica-backend-app bash

# Wykonaj migracje bazy danych
dotnet ef database update

# Wyjdź z kontenera
exit
```

---

## Krok 5: Dostęp do aplikacji

- **Backend API:** http://localhost:8082
- **Swagger UI:** http://localhost:8082/swagger
- **pgAdmin:** http://localhost:8081
    - Email: `admin@guessnica.com`
    - Hasło: `admin`
- **PostgreSQL Database:**
    - Host: `localhost`
    - Port: `5432`
    - Database: `guessnica`
    - Username: `guessnica`
    - Password: `guessnica`

---

## Dodatkowe komendy

### Zatrzymanie środowiska
```bash
docker-compose stop
```

### Ponowne uruchomienie
```bash
docker-compose start
```

### Zakończenie pracy środowiska
```bash
docker-compose down
```

### Całkowite usunięcie środowiska wraz z danymi
```bash
docker-compose down -v
```

### Przebudowa kontenerów po zmianach w kodzie
```bash
docker-compose build --no-cache
docker-compose up -d
```

### Podgląd logów aplikacji
```bash
# Wszystkie logi
docker-compose logs -f

# Tylko logi aplikacji
docker-compose logs -f app

# Tylko logi bazy danych
docker-compose logs -f db
```

### Dostęp do kontenera aplikacji (debugging)
```bash
docker exec -it guessnica-backend-app bash
```

### Wykonanie testów
```bash
docker exec -it guessnica-backend-app dotnet test
```

---

## Struktura projektu

```
Guessnica-backend/
├── Guessnica-backend/           # Główny projekt aplikacji
│   ├── Controllers/             # Kontrolery API
│   ├── Data/                    # DbContext i konfiguracja bazy
│   ├── Dtos/                    # Data Transfer Objects
│   ├── Migrations/              # Migracje Entity Framework
│   ├── Models/                  # Modele danych
│   ├── Services/                # Serwisy biznesowe
│   ├── Program.cs               # Punkt wejścia aplikacji
│   └── appsettings.json         # Konfiguracja aplikacji
├── Guessnica-backend.UnitTests/ # Testy jednostkowe
├── Guessnica-backend.Integration.Test/ # Testy integracyjne
├── docker-compose.yml           # Konfiguracja Docker Compose
├── Dockerfile                   # Definicja obrazu Docker
├── .dockerignore                # Pliki ignorowane przez Docker
├── .env                         # Zmienne środowiskowe (nie commituj!)
└── init.sh                      # Skrypt inicjalizacyjny
```

---

## Rozwiązywanie problemów

### Problem: Port 5432 jest już zajęty
```bash
# Sprawdź, co używa portu
lsof -i :5432

# Zatrzymaj PostgreSQL uruchomiony lokalnie
sudo systemctl stop postgresql
# lub na macOS:
brew services stop postgresql
```

### Problem: Port 8082 jest już zajęty
```bash
# Sprawdź, co używa portu
lsof -i :8082

# Zabij proces zajmujący port (zamień PID na rzeczywisty)
kill -9 PID
```

### Problem: Baza danych nie została zainicjalizowana
```bash
# Usuń wszystko i zacznij od nowa
docker-compose down -v
docker-compose up -d

# Poczekaj 10 sekund
sleep 10

# Wykonaj migracje
docker exec -it guessnica-backend-app dotnet ef database update
```

### Problem: Nie można połączyć się z bazą danych
```bash
# Sprawdź logi bazy danych
docker-compose logs db

# Zrestartuj kontener bazy
docker-compose restart db

# Sprawdź czy baza jest zdrowa
docker exec -it guessnica-db pg_isready -U guessnica
```

### Problem: Aplikacja nie startuje
```bash
# Sprawdź logi aplikacji
docker-compose logs app

# Zweryfikuj zmienne środowiskowe
docker exec -it guessnica-backend-app env | grep -i connection

# Zrestartuj aplikację
docker-compose restart app
```

### Problem: Błąd podczas wykonywania migracji
```bash
# Sprawdź czy wszystkie kontenery działają
docker-compose ps

# Sprawdź connection string
docker exec -it guessnica-backend-app env | grep ConnectionStrings

# Usuń bazę i stwórz od nowa
docker-compose down -v
docker-compose up -d
sleep 10
docker exec -it guessnica-backend-app dotnet ef database update
```

---

## Development lokalny (bez Dockera)

Jeśli chcesz uruchomić aplikację lokalnie bez Dockera:

1. Zainstaluj PostgreSQL lokalnie
2. Utwórz bazę danych `guessnica`
3. Zaktualizuj `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=guessnica;Username=guessnica;Password=guessnica"
  }
}
```
4. Uruchom migracje:
```bash
dotnet ef database update
```
5. Uruchom aplikację:
```bash
dotnet run --project Guessnica-backend
```

Aplikacja będzie dostępna pod adresem: http://localhost:8082

---

## Pliki konfiguracyjne

### .dockerignore
Utwórz plik `.dockerignore` w głównym katalogu:
```
**/.vs
**/.vscode
**/bin
**/obj
**/.git
**/.gitignore
**/docker-compose*
**/Dockerfile*
**/.env
**/README.md
**/Guessnica-backend.UnitTests
**/Guessnica-backend.Integration.Test
```

### .env (przykład)
```bash
POSTGRES_USER=guessnica
POSTGRES_PASSWORD=guessnica
POSTGRES_DB=guessnica
ASPNETCORE_ENVIRONMENT=Development
JWT_KEY=SuperSekretnyKluczJWT1234567890!@#LongSecretKey
FACEBOOK_APP_SECRET=
EMAIL_PASSWORD=
```

---

## Kontakt i wsparcie

W razie problemów lub pytań, utwórz issue na GitHub lub skontaktuj się z zespołem deweloperskim.

---

## Licencja

[Dodaj informacje o licencji projektu]