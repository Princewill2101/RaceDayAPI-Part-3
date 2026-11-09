# RaceDay API — Part 2

**Module:** PROG6212/W – Programming 2B
**Part 2 – RESTful API Development**

RaceDay is a full-stack event management platform for South Africa's road
running, walking, and cycling community. This part contains the RESTful API
built with ASP.NET Core Web API and Entity Framework Core (Code-First).

---

## Roles

| Role | What they can do |
|---|---|
| **Organiser** | Create, edit, and delete events; manage categories; view all enrolments for their events; record participant results. |
| **Participant** | Browse events, enrol under a category, view own enrolments, view own race results. |

Role-based access is enforced at the API level using session management.
Organiser endpoints return `403 Forbidden` when called by a Participant, and
vice versa.

---

## Project structure

```
RaceDayAPI/
├── Controllers/        # One controller per resource area
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── EventsController.cs
│   ├── CategoriesController.cs
│   ├── EnrolmentsController.cs
│   └── ResultsController.cs
├── Models/             # EF Core entity classes (match the Part 1 ERD)
├── DTOs/               # Request and response shapes (no passwords in responses)
├── Data/               # AppDbContext + Code-First configuration
├── Migrations/         # EF Core migration files
├── Services/           # SessionHelper (reads/writes session state)
├── Program.cs          # App startup, DI, Swagger, session, EF migration
└── appsettings.json    # Connection string — update this for your machine

RaceDayAPI.Tests/
└── RaceDayTests.cs     # xUnit tests covering all 4 required areas
```

---

## How to run

### Prerequisites
- Visual Studio 2022 (or VS Code with C# extension)
- .NET 8 SDK
- SQL Server (the same local instance used in Part 1)

### Steps

1. **Clone the repo** and open `RaceDayAPI.sln` in Visual Studio.

2. **Update the connection string** in `RaceDayAPI/appsettings.json`:
   ```json
   "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=RaceDayDb;Trusted_Connection=True;TrustServerCertificate=True;"
   ```
   Replace `YOUR_SERVER_NAME` with the server name shown in SSMS when you
   connect (e.g. `DESKTOP-4BP499D\Prince` or just `localhost`).

3. **Run the project** — press F5 or click the green Run button.
   - EF Core will automatically create and migrate the database on first run.
   - The app opens at the Swagger UI (`https://localhost:{port}/`).

4. **Test the API in Swagger:**
   - Register an Organiser via `POST /api/auth/register`
   - Register a Participant via `POST /api/auth/register`
   - Log in via `POST /api/auth/login` (session cookie is set automatically)
   - Try the protected endpoints — Swagger sends the session cookie for you

### Running the unit tests

In Visual Studio: **Test → Run All Tests**

From the terminal:
```bash
dotnet test RaceDayAPI.sln --verbosity normal
```

All tests use an in-memory database — no SQL Server connection needed.

---

## API endpoints summary

| Method | Route | Role | Description |
|--------|-------|------|-------------|
| POST | /api/auth/register | Public | Create an account |
| POST | /api/auth/login | Public | Log in, starts a session |
| POST | /api/auth/logout | Any | End the session |
| GET | /api/users/me | Any | View own profile |
| PUT | /api/users/me | Any | Update own profile |
| GET | /api/events | Any | List all events |
| GET | /api/events/{id} | Any | Get one event |
| POST | /api/events | Organiser | Create an event |
| PUT | /api/events/{id} | Organiser | Update an event |
| DELETE | /api/events/{id} | Organiser | Delete an event |
| GET | /api/events/{id}/categories | Any | List categories for an event |
| POST | /api/events/{id}/categories | Organiser | Add a category |
| DELETE | /api/categories/{id} | Organiser | Remove a category |
| GET | /api/events/{id}/enrolments | Organiser | View enrolments for an event |
| POST | /api/events/{id}/enrolments | Participant | Enrol in an event |
| GET | /api/enrolments/me | Participant | View own enrolments |
| POST | /api/enrolments/{id}/result | Organiser | Record a result |
| GET | /api/results/me | Participant | View own results |

---

## CI/CD

The GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every push.
It restores packages, builds the solution, and runs all unit tests.

**CI/CD screenshot:** *(paste your green build screenshot here)*

---

## Video walkthrough

**YouTube (unlisted) link:** *(add your video link here)*

---

## AI usage disclosure

*(Note any AI tools used during this part, per the assignment requirement.)*
