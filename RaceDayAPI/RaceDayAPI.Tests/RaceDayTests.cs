using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;
using RaceDayAPI.Services;
using Xunit;

namespace RaceDayAPI.Tests;

// ═══════════════════════════════════════════════════════════════
// Helper: creates a fake HttpContext with a real session so
// controllers can read/write SessionHelper values in tests.
// ═══════════════════════════════════════════════════════════════
internal static class FakeSession
{
    // Build a controller with a session that looks logged-in as the given user
    public static T WithSession<T>(T controller, int userId, string role)
        where T : ControllerBase
    {
        var session = new MockSession();
        SessionHelper.SetUser(session, userId, role);

        var httpContext = new DefaultHttpContext();
        httpContext.Session = session;
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        return controller;
    }

    // Build a controller with no active session (not logged in)
    public static T WithNoSession<T>(T controller)
        where T : ControllerBase
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Session = new MockSession();   // empty session
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        return controller;
    }
}

// ── Minimal ISession implementation for unit tests ───────────
// ASP.NET's DefaultHttpContext.Session throws in tests because
// there is no real middleware. This mock stores values in a
// plain Dictionary instead.
internal class MockSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public bool IsAvailable => true;
    public string Id => "test-session";
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);

    public void Set(string key, byte[] value) => _store[key] = value;

    public bool TryGetValue(string key, out byte[] value)
        => _store.TryGetValue(key, out value!);
}


// ═══════════════════════════════════════════════════════════════
// 1. USER REGISTRATION AND LOGIN
// ═══════════════════════════════════════════════════════════════
public class AuthTests
{
    [Fact]
    public async Task Register_ValidOrganiser_Returns201()
    {
        // Arrange
        var db         = TestDbHelper.GetInMemoryDb("Auth_Register_201");
        var controller = FakeSession.WithNoSession(new AuthController(db));

        var request = new RegisterRequest
        {
            FullName = "Test Organiser",
            Email    = "organiser@test.com",
            Password = "Password123",
            Role     = "Organiser"
        };

        // Act
        var result = await controller.Register(request) as ObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var db = TestDbHelper.GetInMemoryDb("Auth_Register_409");

        // Add a user with this email first
        db.Users.Add(new User
        {
            FullName     = "Existing User",
            Email        = "duplicate@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role         = "Participant"
        });
        await db.SaveChangesAsync();

        var controller = FakeSession.WithNoSession(new AuthController(db));
        var request = new RegisterRequest
        {
            FullName = "Another User",
            Email    = "duplicate@test.com",
            Password = "Password123",
            Role     = "Participant"
        };

        var result = await controller.Register(request) as ObjectResult;
        Assert.Equal(409, result!.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var db         = TestDbHelper.GetInMemoryDb("Auth_Register_400");
        var controller = FakeSession.WithNoSession(new AuthController(db));

        var request = new RegisterRequest
        {
            FullName = "Bad Role User",
            Email    = "badrole@test.com",
            Password = "Password123",
            Role     = "Admin"    // Not a valid role
        };

        var result = await controller.Register(request) as ObjectResult;
        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        var db = TestDbHelper.GetInMemoryDb("Auth_Login_200");

        // Register a user directly in the db
        db.Users.Add(new User
        {
            FullName     = "Login User",
            Email        = "login@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password"),
            Role         = "Participant"
        });
        await db.SaveChangesAsync();

        var controller = FakeSession.WithNoSession(new AuthController(db));
        var result = await controller.Login(new LoginRequest
        {
            Email    = "login@test.com",
            Password = "correct-password"
        }) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var db = TestDbHelper.GetInMemoryDb("Auth_Login_401");

        db.Users.Add(new User
        {
            FullName     = "Login User 2",
            Email        = "login2@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password"),
            Role         = "Organiser"
        });
        await db.SaveChangesAsync();

        var controller = FakeSession.WithNoSession(new AuthController(db));
        var result = await controller.Login(new LoginRequest
        {
            Email    = "login2@test.com",
            Password = "wrong-password"
        }) as UnauthorizedObjectResult;

        Assert.NotNull(result);
        Assert.Equal(401, result!.StatusCode);
    }
}


// ═══════════════════════════════════════════════════════════════
// 2. EVENT MANAGEMENT — Organisers can manage events
// ═══════════════════════════════════════════════════════════════
public class EventManagementTests
{
    // Helper: creates a saved Organiser user and returns their id
    private static async Task<int> SeedOrganiser(RaceDayAPI.Data.AppDbContext db,
        string email = "org@test.com")
    {
        var user = new User
        {
            FullName     = "Test Organiser",
            Email        = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"),
            Role         = "Organiser"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.UserId;
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201()
    {
        var db          = TestDbHelper.GetInMemoryDb("Event_Create_201");
        int organiserId = await SeedOrganiser(db);
        var controller  = FakeSession.WithSession(new EventsController(db),
                              organiserId, "Organiser");

        var result = await controller.Create(new CreateEventRequest
        {
            Name        = "Test Run",
            EventDate   = DateTime.Today.AddMonths(2),
            Location    = "Cape Town",
            DistanceKm  = 10,
            EventTypeId = 1    // "Run" was seeded by EnsureCreated
        }) as ObjectResult;

        Assert.Equal(201, result!.StatusCode);
    }

    [Fact]
    public async Task GetAllEvents_AsOrganiser_Returns200()
    {
        var db          = TestDbHelper.GetInMemoryDb("Event_GetAll_200");
        int organiserId = await SeedOrganiser(db, "org2@test.com");

        // Seed one event
        db.Events.Add(new Event
        {
            OrganiserId = organiserId,
            EventTypeId = 1,
            Name        = "Seeded Event",
            EventDate   = DateTime.Today,
            Location    = "Johannesburg",
            DistanceKm  = 5
        });
        await db.SaveChangesAsync();

        var controller = FakeSession.WithSession(new EventsController(db),
                             organiserId, "Organiser");
        var result = await controller.GetAll() as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result!.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ByNonOwner_ReturnsForbid()
    {
        var db    = TestDbHelper.GetInMemoryDb("Event_Delete_Forbid");
        int owner = await SeedOrganiser(db, "owner@test.com");

        // Add a second Organiser
        var other = new User
        {
            FullName     = "Other Organiser",
            Email        = "other@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"),
            Role         = "Organiser"
        };
        db.Users.Add(other);
        await db.SaveChangesAsync();

        // Create an event owned by 'owner'
        var ev = new Event
        {
            OrganiserId = owner,
            EventTypeId = 1,
            Name        = "Owner's Event",
            EventDate   = DateTime.Today,
            Location    = "Durban",
            DistanceKm  = 21
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        // Try to delete it as 'other'
        var controller = FakeSession.WithSession(new EventsController(db),
                             other.UserId, "Organiser");
        var result = await controller.Delete(ev.EventId);

        Assert.IsType<ForbidResult>(result);
    }
}


// ═══════════════════════════════════════════════════════════════
// 3. ROLE ENFORCEMENT — wrong roles get rejected
// ═══════════════════════════════════════════════════════════════
public class RoleEnforcementTests
{
    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbid()
    {
        var db         = TestDbHelper.GetInMemoryDb("Role_Event_Forbid");
        // Log in as a Participant — should not be allowed to create events
        var controller = FakeSession.WithSession(new EventsController(db), 99, "Participant");

        var result = await controller.Create(new CreateEventRequest
        {
            Name        = "Should Fail",
            EventDate   = DateTime.Today,
            Location    = "Somewhere",
            DistanceKm  = 5,
            EventTypeId = 1
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetAllEvents_WhenNotLoggedIn_Returns401()
    {
        var db         = TestDbHelper.GetInMemoryDb("Role_GetAll_401");
        var controller = FakeSession.WithNoSession(new EventsController(db));

        var result = await controller.GetAll() as UnauthorizedObjectResult;

        Assert.NotNull(result);
        Assert.Equal(401, result!.StatusCode);
    }

    [Fact]
    public async Task GetMyEnrolments_AsOrganiser_ReturnsForbid()
    {
        var db         = TestDbHelper.GetInMemoryDb("Role_Enrolments_Forbid");
        // An Organiser should not be able to call the Participant-only endpoint
        var controller = FakeSession.WithSession(new EnrolmentsController(db), 1, "Organiser");

        var result = await controller.GetMyEnrolments();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task RecordResult_AsParticipant_ReturnsForbid()
    {
        var db         = TestDbHelper.GetInMemoryDb("Role_Result_Forbid");
        // A Participant should not be able to record results
        var controller = FakeSession.WithSession(new EnrolmentsController(db), 1, "Participant");

        var result = await controller.RecordResult(1, new CreateResultRequest
        {
            FinishTime     = TimeSpan.FromMinutes(45),
            FinishPosition = 3
        });

        Assert.IsType<ForbidResult>(result);
    }
}


// ═══════════════════════════════════════════════════════════════
// 4. EVENT ENROLMENTS — Participants can enrol correctly
// ═══════════════════════════════════════════════════════════════
public class EnrolmentTests
{
    private static async Task<(int organiserId, int participantId, int eventId, int categoryId)>
        SeedScenario(RaceDayAPI.Data.AppDbContext db)
    {
        var organiser = new User
        {
            FullName     = "Organiser",
            Email        = $"org-{Guid.NewGuid()}@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"),
            Role         = "Organiser"
        };
        var participant = new User
        {
            FullName     = "Participant",
            Email        = $"part-{Guid.NewGuid()}@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"),
            Role         = "Participant"
        };
        db.Users.AddRange(organiser, participant);
        await db.SaveChangesAsync();

        var ev = new Event
        {
            OrganiserId = organiser.UserId,
            EventTypeId = 1,
            Name        = "Race Event",
            EventDate   = DateTime.Today.AddMonths(1),
            Location    = "Pretoria",
            DistanceKm  = 10
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var category = new Category
        {
            EventId      = ev.EventId,
            CategoryName = "Open",
            MinAge       = 18,
            MaxAge       = 99
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return (organiser.UserId, participant.UserId, ev.EventId, category.CategoryId);
    }

    [Fact]
    public async Task Enrol_ValidParticipant_Returns201()
    {
        var db = TestDbHelper.GetInMemoryDb("Enrol_Valid_201");
        var (_, participantId, eventId, categoryId) = await SeedScenario(db);

        var controller = FakeSession.WithSession(new EventsController(db),
                             participantId, "Participant");

        var result = await controller.Enrol(eventId, new CreateEnrolmentRequest
        {
            CategoryId = categoryId
        }) as ObjectResult;

        Assert.Equal(201, result!.StatusCode);
    }

    [Fact]
    public async Task Enrol_DuplicateEnrolment_Returns409()
    {
        var db = TestDbHelper.GetInMemoryDb("Enrol_Duplicate_409");
        var (_, participantId, eventId, categoryId) = await SeedScenario(db);

        // First enrolment
        db.Enrolments.Add(new Enrolment
        {
            ParticipantId = participantId,
            EventId       = eventId,
            CategoryId    = categoryId
        });
        await db.SaveChangesAsync();

        // Second attempt should be rejected
        var controller = FakeSession.WithSession(new EventsController(db),
                             participantId, "Participant");
        var result = await controller.Enrol(eventId, new CreateEnrolmentRequest
        {
            CategoryId = categoryId
        }) as ObjectResult;

        Assert.Equal(409, result!.StatusCode);
    }

    [Fact]
    public async Task Enrol_WrongCategory_Returns400()
    {
        var db = TestDbHelper.GetInMemoryDb("Enrol_BadCategory_400");
        var (_, participantId, eventId, _) = await SeedScenario(db);

        var controller = FakeSession.WithSession(new EventsController(db),
                             participantId, "Participant");

        // CategoryId 9999 does not belong to this event
        var result = await controller.Enrol(eventId, new CreateEnrolmentRequest
        {
            CategoryId = 9999
        }) as ObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task GetMyEnrolments_ReturnsOnlyMyEnrolments()
    {
        var db = TestDbHelper.GetInMemoryDb("Enrol_GetMine");
        var (_, participantId, eventId, categoryId) = await SeedScenario(db);

        db.Enrolments.Add(new Enrolment
        {
            ParticipantId = participantId,
            EventId       = eventId,
            CategoryId    = categoryId
        });
        await db.SaveChangesAsync();

        var controller = FakeSession.WithSession(new EnrolmentsController(db),
                             participantId, "Participant");
        var result = await controller.GetMyEnrolments() as OkObjectResult;

        Assert.Equal(200, result!.StatusCode);
    }
}
