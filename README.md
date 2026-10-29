# CCI Ago Campus Videography Unit Scheduler

A lightweight, internal web application built for the CCI Ago Campus Videography Unit. It replaces inefficient team-based scheduling with a direct, top-down assignment system managed entirely by the unit leader (Admin).

## 🚀 Live Demo
[https://cci-ago-video-unit.up.railway.app](https://cci-ago-video-unit.up.railway.app)

## 🛠️ Tech Stack
- **Backend:** C# / ASP.NET Core 10 (Razor Pages)
- **Database:** PostgreSQL (via Entity Framework Core)
- **Frontend:** Bootstrap 5, Custom CSS (Rabium Design System)
- **Auth:** Cookie Authentication (BCrypt for Admin, Passwordless for Members)
- **Hosting:** Railway (Docker)

## ✨ Core Features
- **Admin Dashboard:** Visual overview of upcoming services and urgent absentee alerts.
- **Smart Scheduling Grid:** Assign members to specific cameras for 30-minute sessions within a service.
- **Rules Engine:** Automatically enforces the "Max 2 Services per day" and "No duplicate cameras" rules, with an emergency manual override.
- **Member Portal:** Volunteers log in with just their email to see their schedule, mark attendance, and leave reasons for absence.
- **Audit Logging:** Tracks all database changes with user attribution.
- **Concurrency Control:** Prevents two admins from overwriting each other's schedule changes.

## 🔐 Admin Password Recovery (Emergency Only)
If you are the ONLY Admin and you forget your password, you must manually update the database.
1. Go to [bcrypt-generator.com](https://bcrypt-generator.com/).
2. Type your new password, set rounds to 11, and click "Generate".
3. Copy the hash (e.g., `$2a$11$...`).
4. Open your PostgreSQL database and run:

```sql
UPDATE "Users" 
SET "PasswordHash" = 'PASTE_YOUR_HASH_HERE', 
    "MustChangePassword" = false 
WHERE "Email" = 'your-admin-email@example.com';
```
## ⚠️ Security Note
Member login is intentionally passwordless to ensure zero friction for church volunteers. Anyone with a member's email can log in as them. This is an accepted trade-off for a closed, internal volunteer tool.
