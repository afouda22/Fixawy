# Teammate 2 - B2C Booking & Review Engine

Additive module. See the delivery notes for the full file list.

## Database (migration `AddBookingAndReviewEngine`)

IMPORTANT: `Program.cs` already runs `Database.MigrateAsync()` at startup. Starting the app against the shared
server in `appsettings.json` WILL apply this migration to that server. To avoid that, point the app at a local
database first (do not edit appsettings.json):

    dotnet user-secrets init
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=FixawyyBookingDev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

Verify the hand-written migration matches the model (no output of changes expected):

    dotnet ef migrations has-pending-model-changes

Apply manually (recommended for the shared DB, after the team agrees):

    dotnet ef migrations script 20261118212541_AddFacilityToApplicationUser AddBookingAndReviewEngine -o booking.sql
    dotnet ef database update

## Dev mock identity (`DevTesting` in appsettings.Development.json)

Only active when ASPNETCORE_ENVIRONMENT=Development AND nobody is signed in.
`CustomerId` "2", `TechnicianId` 1, `ActAs` = Customer | Technician. Real logins always win.
Set `Enabled` to false (or run in Production) to switch the mock off; the same controllers then use the real user.

## URLs

/Booking/Technicians  /Booking/Create?technicianId=1  /Booking/MyBookings  /Booking/TechnicianBookings  /Review/Technician/1
