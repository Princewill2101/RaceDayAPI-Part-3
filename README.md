# RaceDay - PROG6212/W Programming 2B

RaceDay is a full-stack event management platform for South Africa's road running,
walking and cycling community.

| Part | What it is | Folder |
|------|------------|--------|
| 1 | Planning (ERD, SQL script, API endpoint plan) | `docs/` |
| 2 | RESTful API (ASP.NET Core Web API, EF Core, SQL Server, xUnit) | `RaceDayAPI/` |
| 3 | MVC web app, Azure Blob Storage, Docker, CI/CD | `RaceDayWeb/` |

**YouTube video (unlisted):** _paste your link he_

**CI/CD green build:** _paste your screenshot here_ ("<img width="928" height="451" alt="image" src="https://github.com/user-attachments/assets/afbe8bce-5ac3-4a19-9311-616e777d3ff0" />
" />
`)

---

## Part 3 - what was added

* **`RaceDayWeb/`** - ASP.NET Core MVC app. Every piece of data comes from the Part 2 API
  through one typed client (`Services/RaceDayApiClient.cs`). There is no database access and
  no duplicated business rules in the MVC project.
* **Session-based login** - on login the MVC app stores the user id, role and name in its own
  session and keeps the API's session cookie so later calls run as that user. Logout ends both.
  `[RoleAuthorize]` sends logged-out visitors to the login page and wrong-role users to an
  "access denied" page.
* **Organiser pages** - dashboard, events (create/edit/delete + banner), categories,
  enrolments per event, results entry.
* **Participant pages** - upcoming events with filters, event detail, enrol with a category,
  my enrolments, my results (e.g. "47th out of 312"), profile with picture upload.
* **Azure Blob Storage** - added to the API (`Services/BlobStorageService.cs`,
  `POST /api/events/{id}/banner`, `POST /api/users/me/picture`). The MVC app never talks to
  Azure; it sends the file to the API.
* **Docker** - `RaceDayWeb/Dockerfile` (multi-stage, non-root).
* **CI** - `.github/workflows/ci.yml` builds and tests the API, builds the MVC project and
  builds the Docker image.

### New / changed API endpoints (Part 3 needed them)

| Method | Route | Role | Purpose |
|--------|-------|------|---------|
| GET | `/api/eventtypes` | Any | Event types for the create/edit form |
| GET | `/api/organiser/events` | Organiser | Own events with enrolment counts (dashboard) |
| POST | `/api/events/{id}/banner` | Organiser (owner) | Upload banner to Blob Storage |
| POST | `/api/users/me/picture` | Any | Upload profile picture to Blob Storage |

Also: `EnrolmentResponse` gained `EventId` and `HasResult`, `ResultResponse` gained
`TotalFinishers`, and `Forbid()` now returns a real HTTP 403 (it previously threw a 500
because no authentication scheme was registered).

---

## Running it

### 1. Azure Blob Storage (for image uploads)

Create a Storage Account in the Azure portal and copy its connection string, then in the API folder:

```bash
cd RaceDayAPI/RaceDayAPI
dotnet user-secrets init
dotnet user-secrets set "AzureBlob:ConnectionString" "<your connection string>"
```

Without it the app still runs; image uploads show a friendly "storage not available" message.

### 2. The API (Part 2)

Set your SQL Server in `RaceDayAPI/RaceDayAPI/appsettings.json`, then:

```bash
cd RaceDayAPI/RaceDayAPI
dotnet run --urls http://localhost:5080
```

### 3. The MVC app - locally

```bash
cd RaceDayWeb
dotnet run
```

Open http://localhost:5090. If your API is on another address, change `ApiSettings:BaseUrl`
in `RaceDayWeb/appsettings.json`.

### 4. The MVC app - Docker (single command)

With the API running on port 5080 as above:

```bash
docker build -t raceday-web ./RaceDayWeb && docker run --rm -p 8090:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5080/ raceday-web
```

Open http://localhost:8090. On Linux add `--add-host=host.docker.internal:host-gateway`.

**Everything in containers (optional):** copy `.env.example` to `.env`, fill it in, then
`docker compose up --build` and open http://localhost:8090.

### First use

1. Sign up as an **Organiser**, create an event, add categories.
2. Log out, sign up as a **Participant**, enter the event.
3. Back as the Organiser, open the event's Results page (after its date) and publish a result.
4. As the Participant, check **My Results**.

---

## AI usage disclosure

_Write your own disclosure here, as required by the assignment (what tools you used and for what)._
