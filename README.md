# MALAPAD COGON e-CONCERN
### Community Reporting and Tracking System
**Barangay Malapad, Cogon, Municipality of Sigma, Province of Capiz, Philippines**

---

## 1. Project Purpose & Overview

The **Malapad Cogon e-CONCERN System** is a lightweight, secure, mobile-friendly web application designed specifically for rural community reporting. It replaces scattered paper records and informal text messages with a transparent digital tracker where:
1. **Residents** can report civic issues (damaged roads, clogged drainage, streetlights, agricultural pathways, garbage, and storm hazards) and track progress using a unique **Reference Number** and **4-Digit Secret PIN**.
2. **Authorized Barangay Personnel (Staff & Tanods)** can view assigned tasks, update statuses, write action notes, and upload proof-of-resolution photos.
3. **Barangay Officials & Administrators** can monitor the live operations dashboard, assign personnel, forward issues to municipal agencies (Sigma LGU, CAPELCO, MENRO, DPWH), manage categories/puroks, generate council summary reports, and export CSV spreadsheets.
4. **Walk-In Residents** (such as senior citizens or farmers without internet access) are fully supported through the **Assisted Walk-In Intake Form** at the Barangay Hall.

---

## 2. Technology Stack

* **Framework:** ASP.NET Core MVC (.NET 8.0)
* **Language:** C# 12
* **ORM:** Entity Framework Core 8.0
* **Database:** SQLite (`econcern.db`) for lightweight, zero-maintenance operations (with EF Core migration path to SQL Server/PostgreSQL if desired).
* **Authentication & Roles:** ASP.NET Core Identity with role-based authorization (`Admin`, `Staff`).
* **Frontend:** Bootstrap 5.3, Bootstrap Icons, responsive mobile-first CSS with native print stylesheet support.

---

## 3. Quick Start (Local Run)

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.

### Steps to Run
```powershell
# 1. Navigate to the project directory
cd c:\E-Concern

# 2. Build the application
dotnet build

# 3. Run the web server
dotnet run --launch-profile "http"
```
Open your browser and navigate to:
**`http://localhost:5242`**

---

## 4. Default Seeded Credentials

On first run, the system automatically creates the database and seeds the following official accounts:

| Role | Username / Email | Password | Access Rights |
| :--- | :--- | :--- | :--- |
| **Barangay Administrator** | `admin@malapadcogon.gov.ph` | `Admin@Malapad2026!` | Full access: Dashboard, Workflow, Category & Sitio configuration, Staff account management, Council reports, and Audit trail. |
| **Barangay Desk Officer** | `staff@malapadcogon.gov.ph` | `Staff@Malapad2026!` | Operational access: Dashboard, Walk-in intake, assigned concerns, status updates, and reports. *(Blocked from system settings and user management).* |

---

## 5. Security & Data Privacy (RA 10173 Compliance)

1. **Confidential Complainant Protection:**
   * The public status tracker (`/Concern/Track`) **never** exposes complainant names, telephone numbers, residential markers, or internal staff notes.
   * Accessing a report requires **both** the Reference Number (e.g., `MC-2026-0001`) and the resident's **4-digit PIN**.
2. **Cryptographic Protection:**
   * PINs are hashed using **PBKDF2 with HMAC-SHA256** and unique cryptographic salts.
   * Uploaded evidence images are validated via magic bytes (file signatures) to block disguised executable files, size-restricted to 5 MB, and stored using GUID filenames.
3. **Audit Trail:**
   * Every status change, assignment, municipal forwarding, category edit, and data export is logged to the immutable `AuditLogs` table with actor, IP address, and timestamp.
4. **Data Minimization:**
   * Exported CSV reports for council meetings strip personal complainant information to prevent mass privacy leaks.

---

## 6. Hosting & Deployment Comparison for Rural LGUs

| Hosting Option | Estimated Cost | Pros | Limitations / Considerations |
| :--- | :--- | :--- | :--- |
| **Option A: Barangay Hall Local PC (Intranet + Cloudflare Tunnel)** *(Recommended for Low Budget)* | **Free / ₱0** (uses existing Barangay Hall PC & WiFi) | Database stays 100% inside the barangay hall. Zero cloud hosting fees. Works on local LAN even if internet goes down. | PC must remain powered on during operating hours. |
| **Option B: MonsterASP.NET or SmarterASP.NET** | **Free tier** or ~$3/month (approx ₱170/mo) | Native ASP.NET Core support, automatic HTTPS certificates, SQLite and SQL Server support, zero server management. | Requires modest annual budget from barangay funds. |
| **Option C: Linux Cloud VPS (Render / Railway / DigitalOcean)** | ~$4 to $7/month (approx ₱250-₱400/mo) | High availability, scalable, runs on Docker. | Requires Docker configuration and credit card payment. |

---

## 7. Automated Backup Procedure

To back up the database and uploaded photos before sessions or weekly maintenance:

```powershell
powershell -ExecutionPolicy Bypass -File .\backup.ps1
```
This automatically creates a timestamped copy of `econcern.db` and a zip archive of all uploaded photos in the `backups/` directory.

---

## 8. Go-Live Checklist for Barangay Officials

Before deploying for live public submissions:
* [ ] **Review Sitio / Purok Names:** Access **Settings &rarr; Sitios** and verify the spelling and coverage of all local puroks.
* [ ] **Review Categories:** Validate concern categories with the *Punong Barangay* and *Sangguniang Barangay*.
* [ ] **Change Default Passwords:** Change `Admin@Malapad2026!` and `Staff@Malapad2026!` to strong, confidential passwords.
* [ ] **Brief Desk Officers:** Train barangay secretaries and tanods on the **Assisted Walk-In Intake Form** (`/Admin/CreateAssisted`).
* [ ] **Inform Residents:** Remind residents that personal disputes, marital conflicts, and criminal emergencies must be reported to the *Lupon Tagapamayapa* or *Sigma MPS*, not online.
