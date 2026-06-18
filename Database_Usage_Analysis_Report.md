# Database Usage Analysis Report

**Project:** MBM (MSME Bharat Manch)  
**Analysis date:** 2026-06-18  
**Mode:** READ-ONLY — no source code, configuration, or database objects were modified.

**Scope:**
- Backend: `d:\MBM\backend` (.NET 8 API, Entity Framework Core)
- Frontend: `d:\MBM\frontend` (Angular SPA)
- Databases queried read-only:
  - **MBM** (`ConnectionStrings:ConnectionString`) — application database
  - **RBMAIN** (`ConnectionStrings:ReferralDb`) — external referral/CRM database

**Methodology:**
- Static analysis of controllers, services, repositories, EF models, DTOs, hosted services, SQL scripts, and Angular HTTP calls
- Live inventory via `sys.tables` / `sys.objects` (SELECT only)
- Reference counts = occurrences in `backend/**/*.cs` (ripgrep)
- Confidence: **High** = direct EF/SQL evidence; **Medium** = indirect or legacy path; **Low** = naming/heuristic only

---

## Section 1: Used Tables

### 1A. MBM Database (`dbo` schema) — Application Tables

| Table Name | Schema | Referenced In | Reference Count | Confidence |
| ---------- | ------ | ------------- | --------------- | ---------- |
| Users | dbo | `AppDbContext.cs:11`, `Models/User.cs:6`, `AdminController.cs`, `UserController.cs`, `UserManagementRepository.cs`, `LeadAttributionRepository.cs`, `MemberIdGeneratorService.cs`, `AdminSeederHostedService.cs`, +28 files | 102 | High |
| Plans | dbo | `AppDbContext.cs:16`, `Models/Plan.cs:6`, `PaymentController.cs`, `PaymentActivationService.cs`, `SchemeDiscoveryBootstrapHostedService.cs`, `VendorManagementRepository.cs`, +25 files | 134 | High |
| UserPlans | dbo | `AppDbContext.cs:19`, `SubscriptionExpiryHostedService.cs:46-58`, `SubscriptionReminderHostedService.cs:53-63`, `PaymentActivationService.cs`, `UserManagementRepository.cs`, +12 files | 50 | High |
| CustomerReports | dbo | `AppDbContext.cs:22`, `CustomerReportRepository.cs`, `CustomerReportService.cs`, `SdrReportRepository.cs`, `SchemeDiscoveryService.cs`, `CustomerReportSchemaHostedService.cs` | 48 | High |
| Payments | dbo | `AppDbContext.cs:18`, `PaymentController.cs`, `PaymentActivationService.cs`, `MembershipEmailService.cs`, `UserPlanSchemaHostedService.cs` | 39 | High |
| PaymentOrders | dbo | `AppDbContext.cs:17`, `PaymentController.cs`, `LeadPushService.cs:34-44`, `LeadAttributionRepository.cs`, `PaymentActivationService.cs` | 30 | High |
| Vendors | dbo | `AppDbContext.cs:29`, `VendorManagementRepository.cs`, `Models/Vendor.cs:6`, `docs/sql/vendor_management_schema.sql` | 23 | High |
| UserStatusHistory | dbo | `UserManagementRepository.cs`, `UserManagementService.cs:416`, `UserManagementController.cs:115-123` | 22 | High |
| ContactSubmissions | dbo | `AppDbContext.cs:27,69`, `ContactService.cs`, `EnquiryManagementRepository.cs`, `ContactSchemaBootstrap.cs`, `docs/sql/enquiry_management_schema.sql` | 19 | High |
| LoanApplications | dbo | `AppDbContext.cs:55`, `LoanApplicationService.cs`, `LoanApplicationSchemaBootstrap.cs` | 18 | High |
| SchemeDiscoveryRequests | dbo | `AppDbContext.cs:28`, `SchemeDiscoveryService.cs`, `SchemeDiscoveryBootstrapHostedService.cs:39` | 18 | High |
| UserAuditLog | dbo | `UserManagementRepository.cs`, `UserManagementService.cs:395` | 13 | High |
| PaymentOrderReferrals | dbo | `ReferralService.cs`, `LeadPushService.cs:43-44`, `LeadAttributionRepository.cs`, `ReferralSchemaHostedService.cs` | 13 | High |
| EnquiryStatusHistory | dbo | `EnquiryManagementRepository.cs`, `Models/EnquiryStatusHistory.cs:6`, `docs/sql/enquiry_management_schema.sql:79` | 12 | High |
| ReportAuditLogs | dbo | `ReportAuditRepository.cs`, `ReportAuditService.cs`, `CustomerReportSchemaHostedService.cs:76` | 11 | High |
| VendorPlanMappings | dbo | `VendorManagementRepository.cs`, `Models/VendorPlanMapping.cs:6`, `docs/sql/vendor_management_schema.sql:39` | 10 | High |
| ReportChangeRequests | dbo | `ReportChangeRequestRepository.cs` (7 queries), `ReportChangeRequestService.cs`, `AdminReportsController.cs` | 9 | High |
| MemberIdSequences | dbo | `MemberIdSequenceRepository.cs:33-46` (MERGE), `MemberIdSchemaHostedService.cs:26` | 6 | High |
| ApiExceptionLogs | dbo | `ApiExceptionLogService.cs:43-68`, `ApiExceptionLogSchemaHostedService.cs:24` | 6 | High |
| UserStatusAudit | dbo | `AdminController.cs:960-968` (INSERT only), `DateTimeDefaultsSchemaHostedService.cs:42` | 5 | Medium |
| VendorAuditLogs | dbo | `VendorManagementRepository.cs`, `Models/VendorAuditLog.cs:6` | 4 | High |

**Evidence notes:**
- `Users`, `Plans`, `PaymentOrders`, `Payments`, `UserPlans` form the membership/payment core (`PaymentActivationService.cs`, `PaymentController.cs`).
- `ContactSubmissions` doubles as enquiry source for Enquiry Management (`EnquiryManagementRepository.cs`).
- `ReportChangeRequests` is actively used despite 0 rows in production DB (see row counts in Section 7).

### 1B. RBMAIN Database — Tables Used by MBM Application

| Table Name | Schema | Referenced In | Reference Count | Confidence |
| ---------- | ------ | ------------- | --------------- | ---------- |
| lead_data | dbo | `ReferralDbContext.cs:28`, `LeadData.cs:10`, `LeadPushService.cs`, `LoanApplicationService.cs:213-215` | 15 | High |
| employee_master | dbo | `ReferralDbContext.cs:22`, `EmployeeValidationService.cs:40-43` | 5 (EF) + 2 (ToTable) | High |
| broker_master | dbo | `ReferralDbContext.cs:23`, `EmployeeValidationService.cs` (broker/RBA validation) | 4 (EF) + 3 (ToTable) | High |

**Mapping note:** `EmployeeMaster` model has `[Table("Employee_Master")]` (`Referrals/Models/EmployeeMaster.cs:9`) but EF maps to `employee_master` via `ReferralDbContext.cs:22`. SQL Server default collation may resolve these to the same object; verify on target server.

**External trigger (not in repo):** `lead_data` has database triggers — EF disables OUTPUT clause (`ReferralDbContext.cs:25-28`).

### 1C. Tables Referenced Only in Schema/SQL (Not EF Runtime)

| Table Name | Schema | Referenced In | Reference Count | Confidence |
| ---------- | ------ | ------------- | --------------- | ---------- |
| OtpVerifications | dbo | `DateTimeDefaultsSchemaHostedService.cs:32` (ALTER DEFAULT only) | 1 | Medium |
| ReferralLeadOutbox | dbo | `ReferralSchemaHostedService.cs:42-72`, `Models/ReferralLeadOutbox.cs`, `AppDbContext.cs:21` (DbSet registered; **no LINQ read/write in services**) | 15 (mostly DDL/model) | Medium |

---

## Section 2: Potentially Unused Tables

*Criteria: exists in database, not referenced by application runtime logic (EF LINQ, repositories, services, controllers), stored procedures, views, functions, triggers, or scheduled jobs in this solution.*

### MBM Database

| Table Name | Schema | Reason Marked Unused | Confidence |
| ---------- | ------ | -------------------- | ---------- |
| LeadCustomerAttributions | dbo | Present in DB (0 rows). Defined only in `docs/sql/lead_attribution_schema.sql`. `LeadAttributionRepository` computes attribution at runtime from `Users`, `PaymentOrders`, etc. — no EF model or queries. | High |
| LeadCustomerPaymentHistory | dbo | Same as above — optional snapshot table, 0 rows, SQL script only. | High |
| LeadSourceEvents | dbo | Same as above — optional UTM/campaign capture, 0 rows, SQL script only. | High |
| ReferralLeadOutbox | dbo | Table + DbSet + bootstrap DDL exist. `LeadPushService` pushes directly to RBMAIN `lead_data` and updates `PaymentOrderReferrals.LeadPushedAt` — **never inserts/selects from outbox**. 0 rows in DB. | High |
| OtpVerifications | dbo | OTP handled in-memory (`InMemoryOtpService`, `Program.cs:177`). Only reference is `DateTimeDefaultsSchemaHostedService` ALTER. 0 rows in DB. | High |
| sysdiagrams | dbo | SQL Server Management Studio diagram storage. Zero references in solution. 0 rows. | High |
| UserStatusAudit | dbo | Legacy audit table. Only write path: `AdminController.cs:960` (`PATCH /api/admin/members/{id}/active`). New user management uses `UserStatusHistory` + `UserAuditLog` instead. 0 rows; no read queries. | Medium |

### RBMAIN Database

RBMAIN contains **500+ tables** (legacy CRM). MBM application code references exactly **3** (`employee_master`, `broker_master`, `lead_data`). All other RBMAIN tables are **outside MBM application scope** — used by other systems, not this codebase.

| Scope | Count | Reason |
| ----- | ----- | ------ |
| RBMAIN tables not referenced by MBM code | ~500+ | No matches in `backend/**/*.cs` for table names other than the 3 above |

*Listing individual RBMAIN tables as "unused" would be misleading; they belong to the shared RBMAIN platform.*

---

## Section 3: Verification Required Tables

| Table Name | Schema | DB Row Count | Reason Manual Verification Required | Confidence |
| ---------- | ------ | ------------ | ----------------------------------- | ---------- |
| UserStatusAudit | dbo | 0 | **Audit table** — may be retained for compliance; legacy write path still exists in `AdminController` but superseded by `UserStatusHistory` | Medium |
| UserStatusHistory | dbo | 0 | **Audit/history table** — actively used in code but empty; verify whether production deactivations flow through new User Management API | High |
| ReportChangeRequests | dbo | 0 | **Workflow table** — used in code; empty may mean feature unused in prod, not dead schema | High |
| ReferralLeadOutbox | dbo | 0 | **Integration/outbox table** — schema prepared for async lead push; implementation may be planned/incomplete | High |
| LeadCustomerAttributions | dbo | 0 | **Optional snapshot / staging** — script says "REFERENCE ONLY"; may be deployed for future performance | High |
| LeadCustomerPaymentHistory | dbo | 0 | **Optional denormalized history** — same as above | High |
| LeadSourceEvents | dbo | 0 | **Integration/UTM capture** — future attribution extension point per SQL comments | High |
| OtpVerifications | dbo | 0 | **Migration/legacy table** — predates in-memory OTP; may hold historical data on other environments | High |
| ApiExceptionLogs | dbo | 9 | **Log table** — write-only from `ApiExceptionLogService`; verify retention/archival policy | High |
| ReportAuditLogs | dbo | 7 | **Audit table** — append-only report change trail | High |
| VendorAuditLogs | dbo | 1 | **Audit table** — vendor change trail | High |
| UserAuditLog | dbo | 1 | **Audit table** — user management actions | High |
| EnquiryStatusHistory | dbo | 3 | **Audit table** — enquiry status changes | High |
| sysdiagrams | dbo | 0 | **SSMS tooling** — safe to ignore for application analysis | High |

---

## Section 4: Database Objects Not Used in Application Code

### MBM Database

| Object Type | Object Name | Referenced By Application Code |
| ----------- | ----------- | ------------------------------ |
| Stored Procedure | `sp_alterdiagram` | None — SSMS diagram tooling |
| Stored Procedure | `sp_creatediagram` | None |
| Stored Procedure | `sp_dropdiagram` | None |
| Stored Procedure | `sp_helpdiagramdefinition` | None |
| Stored Procedure | `sp_helpdiagrams` | None |
| Stored Procedure | `sp_renamediagram` | None |
| Stored Procedure | `sp_upgraddiagrams` | None |
| Scalar Function | `fn_diagramobjects` | None |
| View | *(none)* | — |
| Trigger | *(none defined in MBM)* | — |

**Evidence:** `sys.objects` query on MBM database returned only the 8 diagram-related objects above. No `EXEC`, `FromSqlRaw`, or `sp_` calls in `backend/**/*.cs`.

### RBMAIN Database

| Object Type | Object Name | Referenced By MBM Application Code |
| ----------- | ----------- | ------------------------------------ |
| Trigger(s) on `lead_data` | *(not named in repo)* | Indirect only — `ReferralDbContext.cs:25` documents trigger presence; MBM does not reference trigger names |

No stored procedures, views, or functions in RBMAIN are invoked from MBM application code.

### SQL Scripts in Repository (Not Executed by App)

| Script | Objects Defined | Executed by Hosted Service? |
| ------ | --------------- | --------------------------- |
| `docs/sql/vendor_management_schema.sql` | `Vendors`, `VendorPlanMappings`, `VendorAuditLogs` | No — manual execution |
| `docs/sql/enquiry_management_schema.sql` | Alters `ContactSubmissions`, creates `EnquiryStatusHistory` | No — manual execution |
| `docs/sql/lead_attribution_schema.sql` | `LeadCustomerAttributions`, `LeadCustomerPaymentHistory`, `LeadSourceEvents` | No — reference only |

---

## Section 5: Entity Framework Analysis

### DbSet Registration vs Runtime Usage

| DbSet (AppDbContext) | Table | Runtime LINQ Used? | Evidence |
| -------------------- | ----- | ------------------ | -------- |
| Users | Users | Yes | Widespread — controllers, repositories, services |
| MemberIdSequences | MemberIdSequences | Yes | `MemberIdSequenceRepository.cs:33-46` |
| UserStatusAudit | UserStatusAudit | **Write only (legacy)** | `AdminController.cs:960` only |
| UserStatusHistory | UserStatusHistory | Yes | `UserManagementRepository.cs` |
| UserAuditLog | UserAuditLog | Yes | `UserManagementRepository.cs` |
| Plans | Plans | Yes | Payment, vendor, scheme flows |
| PaymentOrders | PaymentOrders | Yes | `PaymentController.cs`, `LeadPushService.cs` |
| Payments | Payments | Yes | `PaymentActivationService.cs` |
| UserPlans | UserPlans | Yes | Subscription hosted services |
| PaymentOrderReferrals | PaymentOrderReferrals | Yes | `ReferralService.cs`, `LeadPushService.cs` |
| **ReferralLeadOutbox** | ReferralLeadOutbox | **No** | DbSet at `AppDbContext.cs:21`; zero `.ReferralLeadOutbox` queries in services |
| CustomerReports | CustomerReports | Yes | Report repositories/services |
| ReportAuditLogs | ReportAuditLogs | Yes | `ReportAuditRepository.cs` |
| ReportChangeRequests | ReportChangeRequests | Yes | `ReportChangeRequestRepository.cs` |
| ApiExceptionLogs | ApiExceptionLogs | Yes | `ApiExceptionLogService.cs` |
| LoanApplications | LoanApplications | Yes | `LoanApplicationService.cs` |
| ContactSubmissions | ContactSubmissions | Yes | `ContactService.cs`, `EnquiryManagementRepository.cs` |
| SchemeDiscoveryRequests | SchemeDiscoveryRequests | Yes | `SchemeDiscoveryService.cs` |
| Vendors | Vendors | Yes | `VendorManagementRepository.cs` |
| VendorPlanMappings | VendorPlanMappings | Yes | `VendorManagementRepository.cs` |
| VendorAuditLogs | VendorAuditLogs | Yes | `VendorManagementRepository.cs` |
| EnquiryStatusHistories | EnquiryStatusHistory | Yes | `EnquiryManagementRepository.cs` |

### ReferralDbContext DbSets

| DbSet | Table | Used? | Evidence |
| ----- | ----- | ----- | -------- |
| EmployeeMaster | employee_master | Yes | `EmployeeValidationService.cs:40` |
| BrokerMaster | broker_master | Yes | `EmployeeValidationService.cs` (RBA path) |
| LeadData | lead_data | Yes | `LeadPushService.cs`, `LoanApplicationService.cs` |

### Entities Without EF Model (DB tables only)

| Table | In DB? | EF Model? |
| ----- | ------ | --------- |
| OtpVerifications | Yes | No |
| LeadCustomerAttributions | Yes | No |
| LeadCustomerPaymentHistory | Yes | No |
| LeadSourceEvents | Yes | No |
| sysdiagrams | Yes | No |

### Repositories — All Registered and Called

| Repository | Registered (`Program.cs`) | Injected By |
| ---------- | ------------------------- | ----------- |
| EnquiryManagementRepository | Line 208 | `EnquiryManagementService` |
| VendorManagementRepository | Line 204 | `VendorManagementService` |
| UserManagementRepository | Line 202 | `UserManagementService` |
| LeadAttributionRepository | Line 206 | `LeadAttributionService` |
| MemberIdSequenceRepository | Line 193 | `MemberIdGeneratorService` |
| SdrReportRepository | Line 199 | `SdrReportService` |
| ReportAuditRepository | Line 189 | `ReportAuditService`, `ReportChangeRequestService` |
| ReportChangeRequestRepository | Line 186 | `ReportChangeRequestService`, `CustomerReportService` |
| CustomerReportRepository | Line 188 | `CustomerReportService`, `ReportChangeRequestService` |

**No orphan repositories found.**

### Services — Registration Status

| Service | Registered? | Notes |
| ------- | ------------- | ----- |
| All `I*` interfaces under `Services/` | Yes | Lines 186-209 in `Program.cs` |
| `PaymentActivationService` | Yes (concrete) | Line 111 |
| `MembershipEmailService` | Yes (concrete) | Line 112 |
| `InvoicePdfService` | Yes (concrete) | Line 113 |
| **`RazorpaySubscriptionService`** | **No** | `Services/RazorpaySubscriptionService.cs` — stub class, never injected |
| `IAppDbContext.ExecuteSqlRaw` | N/A | Wrapper at `AppDbContext.cs:38-45` — **no callers** in solution |

### Hosted Services Touching Database

| Hosted Service | Tables Touched |
| -------------- | -------------- |
| AdminSeederHostedService | Users (seed insert) |
| DateTimeDefaultsSchemaHostedService | Users, OtpVerifications, Plans, PaymentOrders, Payments, UserPlans, UserStatusAudit |
| UserPlanSchemaHostedService | UserPlans, Payments (index) |
| ReferralSchemaHostedService | PaymentOrderReferrals, ReferralLeadOutbox |
| CustomerReportSchemaHostedService | CustomerReports, ReportAuditLogs |
| ApiExceptionLogSchemaHostedService | ApiExceptionLogs |
| MemberIdSchemaHostedService | MemberIdSequences, Users |
| LoanApplicationSchemaHostedService | LoanApplications |
| ContactSchemaHostedService | ContactSubmissions |
| SchemeDiscoveryBootstrapHostedService | SchemeDiscoveryRequests, CustomerReports, Plans |
| SubscriptionExpiryHostedService | UserPlans (status update) |
| SubscriptionReminderHostedService | UserPlans, Users, Plans (read) |
| SmtpWarmupHostedService | None |

---

## Section 6: Dead Code Analysis

### Controllers

| Controller | Route Prefix | Status | Evidence |
| ---------- | ------------ | ------ | -------- |
| AuthController | `api/auth` | **Active** | Called from `auth.service.ts` (OTP) |
| UserController | `api/user` | **Active** | Register, login, profile, password reset |
| AdminController | `api/admin` | **Partially legacy** | Dashboard endpoints active; user/member CRUD superseded by UserManagementController |
| UserManagementController | `api/admin/user-management` | **Active** | Primary admin user CRUD |
| PaymentController | `api/payment` | **Active** | Membership checkout, invoices |
| CustomerReportsController | `api/customer/reports` | **Active** | Member report download |
| AdminReportsController | `api/admin/reports` | **Active** | Admin report upload/history |
| SchemeDiscoveryController | `api/scheme-discovery` | **Active** | Scheme discovery flow |
| ContactController | `api/contact` | **Active** | Contact/callback forms |
| LoansController | `api/loans` | **Active** | Loan application |
| ReferralController | `api/referral` | **Active** | Referral validation |
| VendorManagementController | `api/admin/vendor-management` | **Active** | Vendor admin |
| LeadAttributionController | `api/admin/lead-attribution` | **Active** | Lead attribution admin |
| EnquiryManagementController | `api/admin/enquiry-management` | **Active** | Enquiry admin |

**Unused controllers:** 0 (all 14 have at least one frontend or webhook caller).

### API Endpoints Without Frontend Callers

| Endpoint | Controller | Action | Reason | Confidence |
| -------- | ---------- | ------ | ------ | ---------- |
| `GET /api/admin/reports/customers/{customerId}/subscription` | AdminReportsController | `ValidateSubscription` | No match in `frontend/` | High |
| `POST /api/payment/razorpay/webhook` | PaymentController | `Webhook` | Server-side (Razorpay) — expected | High |
| `GET /api/admin/users` | AdminController | `ListUsers` | Superseded by `/api/admin/user-management/admins` | High |
| `POST /api/admin/users` | AdminController | `CreateAdminUser` | Superseded by user-management | High |
| `DELETE /api/admin/users/{userId}` | AdminController | `DeleteUser` | Superseded | High |
| `PATCH /api/admin/users/{userId}/active` | AdminController | `SetAdminActive` | Superseded | High |
| `GET /api/admin/members` | AdminController | `ListMembers` | Superseded by `/api/admin/user-management/members` | High |
| `PATCH /api/admin/members/{userId}/active` | AdminController | `SetMemberActive` | Superseded — **only path writing `UserStatusAudit`** | High |

**Evidence (legacy frontend methods exist but uncalled):** `auth.service.ts:85-108` defines `adminListUsers`, `adminCreateUser`, etc.; grep shows definitions only, no component imports these methods.

### Angular Pages

| Category | Orphaned Routes? | Notes |
| -------- | ---------------- | ----- |
| Public pages (`pages/`) | No | All routed in `app.routes.ts` |
| User pages (`user/`) | No | login, register, profile, my-plan, forgot-password |
| Admin pages (`admin/`) | No | All admin modules routed |

### Angular Components — Orphaned

| Component | Selector | Evidence |
| --------- | -------- | -------- |
| **BllEventsGrid** | `app-bll-events-grid` | Selector appears only in `bll-events-grid.ts:9` — **never used in any template** |
| **EventDetailsModal** | `app-event-details-modal` | Only referenced from `bll-events-grid.html` |

### Angular Services — Orphaned / Partial

| Service | Status | Evidence |
| ------- | ------ | -------- |
| **BllEventsService** | Orphaned (dead chain) | Only imported by `BllEventsGrid` |
| **AuthService** (6 methods) | Dead methods | `adminListUsers`, `adminSetUserActive`, `adminListMembers`, `adminSetMemberActive`, `adminCreateUser`, `adminDeleteUser` — defined `auth.service.ts:85-108`, no callers |
| **EventsService** | Partial | List page uses static HTML; service used by `event-detail` only |
| **SchemesService** | Partial | List page uses static HTML; service used by `scheme-detail` only |
| **RazorpaySubscriptionService** (backend) | Dead class | Not registered in DI |

### Unused Backend Service Class

| Class | File | Evidence |
| ----- | ---- | -------- |
| RazorpaySubscriptionService | `Services/RazorpaySubscriptionService.cs` | No `AddScoped`/`AddSingleton` in `Program.cs`; no constructor injections |

---

## Section 7: Summary

### Database Inventory

| Metric | MBM | RBMAIN (MBM app scope) |
| ------ | --- | ---------------------- |
| **Total tables** | 27 | 3 used / ~500+ total |
| **Used tables (runtime)** | 21 | 3 |
| **Potentially unused tables** | 7 | N/A (other tables = other systems) |
| **Tables requiring verification** | 14 | 0 |
| **Unused stored procedures (in app code)** | 7 (SSMS diagram SPs) | 0 invoked |
| **Unused views** | 0 in DB | — |
| **Unused functions (in app code)** | 1 (`fn_diagramobjects`) | 0 invoked |
| **Triggers referenced by app** | 0 defined; `lead_data` triggers external | — |

### MBM Table Row Counts (live DB snapshot, 2026-06-18)

| Table | Rows |
| ----- | ---- |
| Users | 17 |
| UserPlans | 17 |
| Payments | 17 |
| PaymentOrders | 31 |
| PaymentOrderReferrals | 31 |
| Plans | 5 |
| ContactSubmissions | 9 |
| SchemeDiscoveryRequests | 9 |
| CustomerReports | 3 |
| ApiExceptionLogs | 9 |
| EnquiryStatusHistory | 3 |
| VendorPlanMappings | 3 |
| Vendors | 1 |
| VendorAuditLogs | 1 |
| UserAuditLog | 1 |
| MemberIdSequences | 1 |
| ReportAuditLogs | 7 |
| *All others listed in Section 2* | 0 |

### Application Layer Summary

| Metric | Count |
| ------ | ----- |
| **Total controllers** | 14 |
| **Unused controllers** | 0 |
| **API endpoints (approx.)** | 95+ |
| **Endpoints without frontend caller** | 8 legacy + 1 admin validation + 1 webhook (intentional) |
| **Registered services (interfaces)** | 27/27 under `Services/` |
| **Unregistered service classes** | 1 (`RazorpaySubscriptionService`) |
| **DbSets with no runtime queries** | 1 (`ReferralLeadOutbox`) |
| **Orphaned Angular components** | 2 (`BllEventsGrid`, `EventDetailsModal`) |
| **Orphaned Angular services** | 1 (`BllEventsService`) |

### Key Findings

1. **No application stored procedures** — all data access is EF Core LINQ, raw DDL in hosted services, or one MERGE in `MemberIdSequenceRepository.cs`.
2. **ReferralLeadOutbox** is scaffolded (table, model, DbSet, indexes) but the outbox pattern is **not implemented**; `LeadPushService` writes directly to RBMAIN.
3. **Lead attribution snapshot tables** exist in production DB (0 rows) but application computes attribution dynamically — tables are optional per `lead_attribution_schema.sql` header comments.
4. **Legacy AdminController user/member APIs** remain active on backend but frontend migrated to `UserManagementController`; `UserStatusAudit` may only receive writes if legacy API is called directly.
5. **RBMAIN** is a shared legacy database; MBM touches only 3 tables — do not treat other RBMAIN tables as MBM technical debt.
6. **Vendor/Enquiry schema** requires manual SQL script execution — no hosted bootstrap (unlike Contact/Loan tables).

---

## Appendix A: API Endpoint → Controller Map (Complete)

| HTTP | Route | Controller |
| ---- | ----- | ---------- |
| POST | `/api/auth/otp/email/send` | AuthController |
| POST | `/api/auth/otp/email/verify` | AuthController |
| POST | `/api/auth/otp/sms/send` | AuthController |
| POST | `/api/auth/otp/sms/verify` | AuthController |
| POST | `/api/user/register` | UserController |
| POST | `/api/user/login` | UserController |
| GET | `/api/user/me` | UserController |
| POST | `/api/user/password/forgot` | UserController |
| POST | `/api/user/password/reset` | UserController |
| POST | `/api/admin/login` | AdminController |
| GET | `/api/admin/dashboard/counts` | AdminController |
| GET | `/api/admin/dashboard/details/{category}` | AdminController |
| GET/POST/PATCH/DELETE | `/api/admin/users...` | AdminController (legacy) |
| GET/PATCH | `/api/admin/members...` | AdminController (legacy) |
| GET | `/api/customer/reports` | CustomerReportsController |
| GET | `/api/customer/reports/{id}/download` | CustomerReportsController |
| *15 routes* | `/api/admin/reports/*` | AdminReportsController |
| *5 routes* | `/api/scheme-discovery/*` | SchemeDiscoveryController |
| *9 routes* | `/api/payment/*` | PaymentController |
| *11 routes* | `/api/admin/user-management/*` | UserManagementController |
| *11 routes* | `/api/admin/vendor-management/*` | VendorManagementController |
| *5 routes* | `/api/admin/lead-attribution/*` | LeadAttributionController |
| *6 routes* | `/api/admin/enquiry-management/*` | EnquiryManagementController |
| POST | `/api/contact/submit` | ContactController |
| POST | `/api/contact/callback` | ContactController |
| POST | `/api/loans/apply` | LoansController |
| POST | `/api/referral/validate` | ReferralController |

---

## Appendix B: Raw SQL / DDL Locations

| File | Lines | Operation |
| ---- | ----- | --------- |
| `Auth/LoanApplicationSchemaBootstrap.cs` | 24-82 | CREATE/DROP `LoanApplications`, index |
| `Auth/ContactSchemaBootstrap.cs` | 21-56 | CREATE `ContactSubmissions`, index |
| `Auth/ApiExceptionLogSchemaHostedService.cs` | 24-43 | CREATE `ApiExceptionLogs` |
| `Auth/MemberIdSchemaHostedService.cs` | 26-49 | CREATE `MemberIdSequences`, ALTER `Users` |
| `Auth/UserPlanSchemaHostedService.cs` | 44 | ALTER `UserPlans`, index on `Payments` |
| `Auth/ReferralSchemaHostedService.cs` | 29-80 | CREATE `PaymentOrderReferrals`, `ReferralLeadOutbox` |
| `Auth/CustomerReportSchemaHostedService.cs` | 29-100 | CREATE `CustomerReports`, `ReportAuditLogs` |
| `Auth/SchemeDiscoveryBootstrapHostedService.cs` | 39-91 | CREATE `SchemeDiscoveryRequests`, ALTER `CustomerReports` |
| `Auth/DateTimeDefaultsSchemaHostedService.cs` | 32-63 | ALTER DEFAULT on 7 tables |
| `Services/Repository/MemberIdSequenceRepository.cs` | 33-46 | MERGE `MemberIdSequences` |
| `Services/LoanApplicationService.cs` | 213-215 | `SCOPE_IDENTITY()` after `lead_data` insert |

---

*End of report. Generated by static analysis and read-only database inventory. No assumptions beyond evidenced code and database metadata.*
