================================================================================
# FixFlow — Smart Maintenance & Service Booking Platform
Track: Software Development (.NET & CS3 , HTML5 , React.Js)
Digital Egypt Pioneers Initiative (DEPI)
================================================================================

--------------------------------------------------------------------------------
1. Project Name
--------------------------------------------------------------------------------
FixFlow (Smart Maintenance & Service Booking Platform)


--------------------------------------------------------------------------------
2. Project Idea
--------------------------------------------------------------------------------
FixFlow is an all-in-one web platform that revolutionizes maintenance management 
by serving two distinct market segments:

1. B2C (Customer Marketplace):
   Allows everyday customers to easily find, compare, book, and rate certified 
   technicians (plumbers, electricians, carpenters, AC specialists) for home repairs.

2. B2B (Facility Ticketing & Maintenance Management):
   Empowers organizations (universities, corporate offices, shopping malls) with 
   a centralized CMMS to submit maintenance tickets, assign tasks, track repair 
   lifecycles, and monitor maintenance staff performance.

Key Features:
- Service Booking & Scheduling: Search technician profiles, ratings, and book slots.
- Maintenance Ticketing: Submit complaints with location, photos, and issue description.
- Smart Prioritization: Automatically classify tickets by urgency (Low, Medium, High, Emergency).
- Technician Assignment: Assign requests based on technician specialization and availability.
- Role-Based Access Control (RBAC): Separate roles for Customer, Technician, Employee, Manager, Admin.
- Real-Time Updates (SignalR): Instant live notifications and status changes without page refresh.
- Analytics & Dashboards: Visual reports on completion times, technician ratings, and ticket statuses.


--------------------------------------------------------------------------------
3. Team Members
--------------------------------------------------------------------------------
| Name                 |
|----------------------|
| Abdelrahman Mohamed Galal Gabr  | 
| Abdelrahman Ahmed Fouad         | 
| Bassel Amr Mahmoud              | 
| Mohamed Rashed Ramah            | 


--------------------------------------------------------------------------------
4. Work Plan & Milestones
--------------------------------------------------------------------------------
The project was executed following Agile methodologies across 5 planned sprints:

Sprint 1 (Weeks 1-2): 
- Requirement gathering, ERD modeling, Database schema design, UI/UX Wireframing.

Sprint 2 (Weeks 3-4): 
- ASP.NET Core Clean Architecture setup, Identity & JWT Authentication, Base CRUD APIs.

Sprint 3 (Weeks 5-6): 
- Core business logic (Booking module, Ticket creation, Assignment logic), React page layouts.

Sprint 4 (Weeks 7-8): 
- SignalR integration for instant notifications, interactive dashboards, reporting charts.

Sprint 5 (Weeks 9-10): 
- Integration testing, Swagger API documentation, bug fixing, and final deployment.

--------------------------------------------------------------------------------
5. KPIs (Key Performance Indicators)
--------------------------------------------------------------------------------
Technical KPIs:
- API Response Time: Sub-200ms average response time for standard queries.
- System Availability: High reliability with structured Global Exception Handling.
- Code Quality: Separation of concerns using Clean Architecture and SOLID principles.
- Security: 100% authenticated endpoints via JWT with strict role validation.

Business & Functional KPIs:
- Assignment Efficiency: Reduction in manual ticket assignment time via smart suggestion rules.
- Resolution Tracking: Real-time visibility into the status of 100% of open maintenance tickets.
- User Satisfaction: Average service rating feedback score tracking (Target: > 4.5 / 5.0).


--------------------------------------------------------------------------------
6. Instructor & Supervision
--------------------------------------------------------------------------------
- Instructor / Mentor Name: Eng. [اسم المدرب / الباشمهندس المشرف في DEPI]
- Track: Software Development (.NET & React Track)
- Initiative: Digital Egypt Pioneers Initiative (DEPI) - MCIT


--------------------------------------------------------------------------------
7. Project Files & Directory Structure
--------------------------------------------------------------------------------
fixflow-platform/
├── backend/                  # ASP.NET Core Web API Solution
│   ├── FixFlow.Domain/       # Entities, Enums, Interfaces
│   ├── FixFlow.Application/  # DTOs, Services, Business Logic, Validations
│   ├── FixFlow.Infrastructure/ # EF Core, DbContext, Migrations, Repositories
│   └── FixFlow.API/          # Controllers, Middlewares, SignalR Hubs, Program.cs
├── frontend/                 # React.js Client Application
│   ├── public/               # Static assets & icons
│   └── src/
│       ├── components/       # Reusable UI widgets, Modals, Navbar, Sidebar
│       ├── pages/            # Customer Portal, Dashboards, Booking flow
│       ├── services/         # Axios API clients & SignalR connection
│       └── context/          # Auth Context & State Management
├── docs/                     # Project Documentation
│   ├── ERD-Diagram.png       # Database Entity-Relationship Diagram
│   └── System-Architecture.png # Architecture & Workflow Diagrams
└── README.md                 # Project Overview & Guidelines


--------------------------------------------------------------------------------
How to Run Locally
--------------------------------------------------------------------------------
Prerequisites:
- .NET 8.0 / 9.0 SDK
- Node.js (v18+)
- Microsoft SQL Server

Backend Setup:
1. cd backend/FixFlow.API
2. dotnet restore
3. dotnet ef database update --project ../FixFlow.Infrastructure
4. dotnet run
* Swagger UI: https://localhost:7001/swagger

Frontend Setup:
1. cd frontend
2. npm install
3. npm run dev
================================================================================
