# 🛠️ Fixawyy — Smart Maintenance & Service Booking Platform
### Teammate 1: User Identity, Roles & Technician Directory System
> **Architecture:** Standard ASP.NET Core MVC (Controllers, Razor Views, Models, ViewModels, EF Core)  
> **Framework:** .NET 10.0 (C#)  
> **Track:** Software Development (.NET Track) — DEPI

---

## 📌 1. Solution Structure (Solution Explorer)

```text
Fixawyy/
├── Properties/               # launchSettings.json
├── wwwroot/                  # CSS, JS, Assets (Modern Bootstrap 5 + Bootstrap Icons)
├── Context/                  # AppDbContext.cs, DbInitializer.cs (Seed Data)
├── Controllers/              # HomeController, AccountController, AdminController, TechnicianController
├── Migrations/               # EF Core Migrations (Auto-applied)
├── Model Views/              # ViewModels (Login, Register, UserManagement, TechnicianProfile, Schedules)
├── Models/                   # Domain Entities (ApplicationUser, TechnicianProfile, Specialty, Schedules)
├── Services/                 # IUserService, UserService, ITechnicianService, TechnicianService
├── Views/                    # Razor Views (.cshtml)
│   ├── Account/              # Login.cshtml, Register.cshtml, AccessDenied.cshtml
│   ├── Admin/                # Users.cshtml (Role Management Table & Status Toggle)
│   ├── Technician/           # Directory.cshtml, Profile.cshtml, Schedule.cshtml
│   ├── Home/                 # Index.cshtml (Dashboard Landing & Quick Demo Login)
│   └── Shared/               # _Layout.cshtml, _ValidationScriptsPartial.cshtml
├── Fixawyy.csproj            # Executable project file (Single-click run)
└── Fixawyy.sln               # Visual Studio Solution File
```

---

## 🔑 2. Default Seeded Accounts

When the application runs, the database (`FixawyyMvcDb`) is automatically created, migrated, and seeded with:

| Account Role | Email | Password |
| :--- | :--- | :--- |
| **Admin** | `admin@fixawyy.com` | `Admin@123456` |
| **Technician** | `technician@fixawyy.com` | `Tech@123456` |

---

## 🚀 3. How to Run from Visual Studio

1. Double click **`Fixawyy.sln`** (or **`Fixawyy.csproj`**) to open directly in Visual Studio.
2. Press **`F5`** or click the green **`https`** Run button at the top toolbar.
3. The browser will open automatically and the whole app is live! No separate Node or Terminal commands needed.
