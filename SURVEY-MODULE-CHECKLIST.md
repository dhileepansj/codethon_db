# Survey Module — Feature Checklist

## Phase 1: Core (Domain + Database)

### Enums
- [x] SurveyStatus (Draft, Active, Closed, Archived)
- [x] SurveyFieldType (ShortText, LongText, Number, Email, Phone, Date, DateTime, Time, Dropdown, MultiSelect, Radio, Checkbox, Rating, Scale, FileUpload, Section, Paragraph, YesNo, Matrix)
- [x] DependencyCondition (Equals, NotEquals, Contains, GreaterThan, LessThan, IsEmpty, IsNotEmpty)
- [x] DependencyAction (Show, Hide, Require, SetOptions)
- [x] ParticipantStatus (Pending, Sent, Reminded, Responded, Declined)
- [x] EmailStatus (Pending, Sent, Failed, Bounced)
- [x] DeclinedBy (ReportingManager, VerticalHead, Self)

### Domain Entities
- [x] Survey
- [x] SurveyField
- [x] SurveyFieldDependency
- [x] SurveyParticipant
- [x] SurveyDistribution
- [x] SurveyResponse
- [x] SurveyResponseAnswer
- [x] SurveyEmailSettings
- [x] SurveyParticipantStatusLog (Decline tracking with attachment)
- [x] SurveyReminderLog
- [x] SurveyOtp

### Repository Interfaces
- [x] ISurveyRepository
- [x] ISurveyFieldRepository
- [x] ISurveyParticipantRepository
- [x] ISurveyDistributionRepository
- [x] ISurveyResponseRepository
- [x] ISurveyOtpRepository

### Repository Implementations
- [x] SurveyRepository
- [x] SurveyFieldRepository
- [x] SurveyParticipantRepository
- [x] SurveyDistributionRepository
- [x] SurveyResponseRepository
- [x] SurveyOtpRepository

### DbContext Registration
- [x] Add all Survey DbSets to HackathonDbContext
- [x] Add OnModelCreating configurations (indexes, relationships)

### Application Layer — DTOs
- [x] SurveyDto, CreateSurveyDto, UpdateSurveyDto
- [x] SurveyFieldDto, CreateFieldDto, UpdateFieldDto
- [x] FieldDependencyDto, CreateDependencyDto
- [x] ParticipantDto, BulkUploadDto
- [x] DistributionDto, EmailSettingsDto
- [x] ResponseDto, ResponseAnswerDto
- [x] SurveyDashboardDto

### Application Layer — Service Interfaces
- [x] ISurveyService
- [x] ISurveyFormBuilderService
- [x] ISurveyDistributionService
- [x] ISurveyResponseService
- [x] ISurveyDashboardService

### Application Layer — Service Implementations
- [x] SurveyService (CRUD, clone, status management)
- [x] SurveyFormBuilderService (fields, dependencies, reorder)
- [x] SurveyDistributionService (bulk upload, email send, remind)
- [x] SurveyResponseService (OTP, verify, submit, one-time check)
- [x] SurveyDashboardService (stats, analytics, export)

### API Controllers
- [x] SurveyController (CRUD, clone, status)
- [x] SurveyFieldController (field CRUD, reorder, dependencies)
- [x] SurveyParticipantController (upload, list, decline, reset)
- [x] SurveyDistributionController (email settings, send, remind)
- [x] SurveyRespondController (public: verify email, OTP, submit)
- [x] SurveyDashboardController (stats, responses, export)

### DI Registration
- [x] Register all repositories in DI container
- [x] Register all services in DI container

---

## Phase 2: Dependent Fields

### Backend
- [x] FieldDependency CRUD endpoints working
- [x] Circular dependency detection (validation)
- [x] Dynamic options (SetOptions action) storage
- [x] Multi-condition support (AND/OR logic)

### Frontend
- [x] DependencyEngine (evaluate show/hide/require at runtime)
- [x] DependencyEditor UI in form builder
- [x] Cascading dropdown support (dynamic option filtering on respond page)
- [x] Visual "Logic" badge on dependent fields
- [x] Circular dependency warning in builder (backend validates, UI calls validate endpoint)

---

## Phase 3: Distribution + OTP

### Backend
- [x] Bulk CSV/Excel upload with validation
- [x] Email template rendering (variable substitution)
- [x] SMTP email sending (MailKit integration)
- [x] OTP generation (6-digit, hashed, 5-min expiry)
- [x] OTP verification (3 attempts, 15-min lockout)
- [x] Rate limiting on OTP resend (max 3)
- [x] Token-based survey access (unique per participant)
- [x] One-time submission enforcement (409 on duplicate)
- [x] Auto-fill participant info after OTP verification

### Frontend
- [x] Bulk upload UI with CSV/Excel drag-drop
- [x] Upload preview table with validation errors
- [x] Template download button
- [ ] Email settings form (RM toggle, VH toggle, CC list)
- [ ] Email subject/body template editor with variable chips
- [ ] Email preview modal
- [x] Send/Remind buttons with confirmation
- [x] Public survey response page — email entry step
- [x] OTP verification step (6-digit input with resend)
- [x] Auto-fill display (Employee ID, Name, Email — read-only)
- [x] "Already submitted" message
- [x] "Not registered" / "Declined" error messages

---

## Phase 4: Dashboard + Decline Management

### Backend
- [x] Dashboard summary stats endpoint (total, responded, pending, declined)
- [x] Per-field analytics endpoint (charts data)
- [x] Individual response detail endpoint
- [x] Excel/CSV export endpoint
- [x] Decline participant endpoint (with reason + attachment upload)
- [x] Reset participant status endpoint
- [x] Reminder log endpoint
- [x] Selective reminder (send to selected pending participants)
- [ ] Auto-reminder background service (optional)

### Frontend
- [x] Survey list page (all surveys with status badges)
- [x] Survey dashboard — summary cards (donut chart, progress bar)
- [x] Field-wise analytics (bar charts for MCQ, averages for rating)
- [x] Response table (sortable, filterable, expandable)
- [x] Individual response detail view
- [x] Export button (Excel/CSV)
- [x] Participant table with status column
- [x] "Mark Declined" modal (reason + file upload)
- [x] "Send Reminder" with multi-select pending list
- [ ] Reminder history view
- [ ] Declined list with attachment download

---

## Phase 5: Form Builder UI

### Frontend
- [x] Form builder page layout (palette | canvas | properties)
- [x] Field palette — all field types grouped by category
- [x] Drag-and-drop from palette to canvas (native drag)
- [x] Drag-to-reorder fields on canvas
- [x] Inline question editing (click to edit label)
- [x] Field properties panel (label, helper text, required, validation)
- [x] Option list editor for choice fields (add/remove/reorder)
- [x] "Add Other" option button
- [ ] Section/page break support (field type exists, multi-page nav not yet)
- [x] Real-time preview (field preview in canvas)
- [x] Preview as respondent (full page with desktop/mobile toggle)
- [x] Auto-save with "last saved" indicator
- [x] Undo/Redo (Ctrl+Z / Ctrl+Y with 30-step history)
- [x] Delete field (with dependency warning logic in backend)
- [x] Duplicate field
- [ ] Survey settings (title, description, thank-you message, allow multiple)
- [x] Clone survey feature
- [x] Survey templates (4 starter templates: Feedback, Event, Training, Employee Satisfaction)

---

## Phase 6: Polish & Integration

- [x] Mobile-responsive response form (responsive classes used)
- [x] Survey status lifecycle (Draft → Active → Closed → Archived)
- [ ] Multi-survey concurrent view on admin dashboard
- [ ] Email delivery status tracking
- [ ] Navigation integration (add Survey section to admin sidebar)
- [x] Route protection (admin-only for builder/dashboard)
- [ ] Error handling & loading states throughout
- [ ] Toast notifications for all actions
