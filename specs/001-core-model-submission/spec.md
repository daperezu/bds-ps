# Feature Specification: Core Data Model & Application Submission

**Feature Branch**: `001-core-model-submission`
**Created**: 2026-04-15
**Status**: Draft
**Input**: Funding Request & Evaluation Platform — core data model and application submission workflow (first of several specs)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Applicant Creates and Submits a Funding Application (Priority: P1)

An entrepreneur logs in, creates a new funding application, adds one or more line items with product details, categories, technical specs, and structured impact definitions, attaches supplier quotations for each item, and submits the application for review. The system validates that all business rules are met before accepting the submission.

**Why this priority**: This is the core value proposition — without application submission, nothing else in the platform works. It replaces the manual Excel-based process and is the foundation for all downstream workflows (review, approval, payment).

**Independent Test**: Can be fully tested by registering an applicant, creating an application with items/suppliers/quotations/impact, and submitting it. Delivers the primary value of structured, validated funding request capture.

**Acceptance Scenarios**:

1. **Given** an authenticated applicant with no existing applications, **When** they create a new application and add one item with the minimum required quotations and a completed impact definition, **Then** the application is created in Draft state and transitions to Submitted upon submission.
2. **Given** an authenticated applicant with a draft application, **When** they attempt to submit without meeting the minimum quotation count on one item, **Then** the submission is rejected with a clear error message and the application remains in Draft state.
3. **Given** an authenticated applicant with a draft application, **When** they attempt to submit with an incomplete impact definition on one item, **Then** the submission is rejected with a clear error listing all validation failures at once.

---

### User Story 2 - Applicant Saves Draft and Returns Later (Priority: P1)

An applicant begins filling out a funding application but is not ready to submit. They save their progress and return later to continue editing before submitting.

**Why this priority**: Applications are complex and require gathering quotations from multiple suppliers. Applicants cannot be expected to complete everything in one session. Draft persistence is essential for usability.

**Independent Test**: Can be tested by creating an application, adding partial data, navigating away, returning, and verifying all previously entered data is intact.

**Acceptance Scenarios**:

1. **Given** an authenticated applicant working on a draft application, **When** they save and log out, **Then** all entered data (items, suppliers, quotations, impact) is persisted and available when they log back in.
2. **Given** an applicant with a saved draft, **When** they return and add more items or modify existing ones, **Then** changes are saved and the application can be submitted when complete.

---

### User Story 3 - Applicant Manages Items Within an Application (Priority: P1)

An applicant adds, edits, and removes line items within a draft application. Each item requires a product name, category selection, technical specifications, impact definition, and supplier quotations.

**Why this priority**: Items are the core unit of a funding request. The ability to manage them flexibly is fundamental to application composition.

**Independent Test**: Can be tested by creating an application, adding multiple items with varying categories and specs, editing one item, removing another, and verifying the application reflects the changes.

**Acceptance Scenarios**:

1. **Given** a draft application, **When** the applicant adds an item with a product name, category, technical specifications, and a completed impact definition, **Then** the item is saved and displayed within the application.
2. **Given** a draft application with three items, **When** the applicant removes one item, **Then** only two items remain and the removed item's quotations and documents are also deleted.
3. **Given** a draft application with an item, **When** the applicant edits the product name and technical specs, **Then** the changes are saved and reflected immediately.

---

### User Story 4 - Applicant Attaches Supplier Quotations (Priority: P1)

For each item in the application, the applicant adds suppliers and uploads quotation documents. The system enforces the configurable minimum number of quotations per item.

**Why this priority**: Supplier quotations are a core business requirement — the SRS mandates multiple quotations per item for competitive evaluation. Without this, applications cannot be submitted.

**Independent Test**: Can be tested by adding suppliers to an item, uploading quotation files, and verifying the minimum count is enforced at submission time.

**Acceptance Scenarios**:

1. **Given** an item with no suppliers, **When** the applicant adds a supplier with a quotation document (price, validity, uploaded file), **Then** the supplier and quotation are saved and linked to the item.
2. **Given** an item with one supplier and the system minimum is two, **When** the applicant attempts to submit the application, **Then** submission is rejected with an error indicating insufficient quotations for that item.
3. **Given** an item with a supplier quotation already attached, **When** the applicant replaces the quotation document, **Then** the old file is deleted from disk and the new file is stored with updated metadata.
4. **Given** an item, **When** the applicant tries to add the same supplier (by Legal ID) a second time to the same item, **Then** the system prevents the duplicate and displays an error.

---

### User Story 5 - Applicant Fills Impact Using Dynamic Templates (Priority: P2)

For each item, the applicant selects an impact type from a list of database-configurable templates, then fills in the required parameters. Different impact types have different parameter shapes (e.g., percentage + timeframe, currency amount + category).

**Why this priority**: Impact is required for submission, but the template configuration system is more complex than the core CRUD operations. It adds significant value by making impact definitions structured and consistent.

**Independent Test**: Can be tested by selecting different impact templates, filling in parameters, and verifying the data is saved correctly and validated at submission.

**Acceptance Scenarios**:

1. **Given** an item with no impact defined, **When** the applicant selects the "Increase Production Capacity" template, **Then** the system displays input fields for percentage and timeframe.
2. **Given** an item with a selected impact template, **When** the applicant fills all required parameters and saves, **Then** the impact is stored with the template reference and parameter values.
3. **Given** an item with a selected impact template, **When** the applicant leaves a required parameter empty and tries to submit, **Then** submission is rejected with an error identifying the missing impact parameter.
4. **Given** an impact template with both required and optional parameters, **When** the applicant fills only the required ones, **Then** the impact is considered complete and submission succeeds.

---

### User Story 6 - Administrator Manages System Configuration (Priority: P2)

An administrator configures system-wide settings such as the minimum number of quotations per item and allowed file upload types. These settings are stored in the database and take effect immediately.

**Why this priority**: Configuration drives business rule enforcement. Without it, rules would be hardcoded. It's lower priority because sensible defaults allow the system to function initially.

**Independent Test**: Can be tested by changing the minimum quotation setting and verifying that subsequent submission validations respect the new value.

**Acceptance Scenarios**:

1. **Given** the minimum quotation setting is 2, **When** an admin changes it to 3, **Then** all subsequent application submissions require 3 quotations per item.
2. **Given** a draft application that met the old minimum of 2, **When** the minimum is changed to 3 and the applicant submits, **Then** submission is rejected because the new minimum is not met.

---

### User Story 7 - Administrator Manages Impact Templates (Priority: P2)

An administrator creates, edits, and manages impact templates that define the structured input forms applicants use when specifying the impact of each requested item.

**Why this priority**: Impact templates must exist before applicants can fill them in. This is the admin-facing side of the dynamic impact system.

**Independent Test**: Can be tested by creating a new impact template with parameter definitions and verifying it appears as an option for applicants when defining item impact.

**Acceptance Scenarios**:

1. **Given** the system has two existing impact templates, **When** an admin creates a new template with name "Reduce Operating Costs" and parameters (percentage: decimal required, timeframe: integer required, cost category: text optional), **Then** the template is saved and available for applicant selection.
2. **Given** an impact template used by existing draft applications, **When** an admin modifies the template by adding a new required parameter, **Then** existing drafts that used this template show a validation error on their next submission attempt, identifying the missing parameter.

---

### User Story 8 - User Registration and Authentication (Priority: P2)

Users (applicants and staff) register for accounts and log in to access the system. Authentication is required for all operations.

**Why this priority**: Authentication gates access to all functionality. It's P2 because it's standard infrastructure — ASP.NET Identity handles most of it out of the box.

**Independent Test**: Can be tested by registering a new user, logging in, accessing a protected page, logging out, and verifying protected pages are inaccessible without login.

**Acceptance Scenarios**:

1. **Given** an unregistered user, **When** they complete the registration form with valid details, **Then** an account is created and they can log in.
2. **Given** a registered user, **When** they enter correct credentials, **Then** they are authenticated and directed to the application dashboard.
3. **Given** an unauthenticated user, **When** they attempt to access the application submission page, **Then** they are redirected to the login page.

---

### Edge Cases

- Applicant starts an application but never submits — draft persists indefinitely (no auto-cleanup in this spec)
- Applicant removes all items from a draft, then tries to submit — validation rejects with "at least one item required"
- A supplier is linked to multiple items within the same application — allowed, each item has its own quotation
- Admin changes minimum quotation count while applications are in Draft — new value applies on next submission attempt
- Applicant uploads a file that exceeds the maximum size — upload is rejected with a clear error, no partial file is stored
- Applicant uploads a file with an unsupported type — upload is rejected with a message listing allowed types
- Impact template is modified after an applicant selected it but before submission — saved parameter values are validated against the current template definition; mismatches surface as validation errors
- Two browser tabs editing the same application — optimistic concurrency (row version) detects conflicts and warns the user

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow authenticated applicants to create new funding applications in Draft state
- **FR-002**: System MUST allow applicants to add, edit, and remove items (lines) within a draft application
- **FR-003**: Each item MUST have a product name (free text), a selected category, technical specifications, and a completed impact definition
- **FR-004**: System MUST support database-configurable impact templates with named parameter slots, data types (text, decimal, integer, date), and required/optional flags
- **FR-005**: System MUST render dynamic input forms based on the selected impact template's parameter definitions
- **FR-006**: System MUST allow applicants to add suppliers to each item, including legal ID, contact info, location, invoice capability, shipping details, warranty info, and compliance status
- **FR-007**: System MUST allow applicants to upload quotation documents per supplier per item, recording price, validity period, and file metadata
- **FR-008**: System MUST store uploaded documents on the local file system and track metadata (original filename, size, content type, storage path, upload timestamp) in the database
- **FR-009**: System MUST enforce a configurable minimum number of supplier quotations per item at submission time
- **FR-010**: System MUST validate that all items have completed impact definitions before allowing submission
- **FR-011**: System MUST validate that all required fields are populated before allowing submission
- **FR-012**: System MUST collect and display all validation errors at once (not one at a time) when submission fails
- **FR-013**: System MUST transition the application state from Draft to Submitted upon successful validation
- **FR-014**: System MUST persist all draft data so applicants can save progress and return later
- **FR-015**: System MUST prevent adding the same supplier (by Legal ID) twice to the same item
- **FR-016**: When a quotation document is replaced, the system MUST delete the old file from disk and update metadata
- **FR-017**: System MUST store system-wide configuration settings (minimum quotations, allowed file types, max file size) in the database
- **FR-018**: System MUST support user registration and authentication via ASP.NET Identity
- **FR-019**: System MUST maintain version history on applications, tracking who changed what and when
- **FR-020**: System MUST use optimistic concurrency (row version) to detect concurrent edits and warn the user
- **FR-021**: System MUST support configurable allowed file types and maximum file size for uploads, enforced at upload time
- **FR-022**: Administrators MUST be able to create, edit, and manage impact templates and their parameter definitions
- **FR-023**: Administrators MUST be able to view and modify system configuration settings
- **FR-024**: Every functional requirement MUST have corresponding Playwright end-to-end tests that validate the user flow through the browser

### Key Entities

- **Applicant**: Represents an entrepreneur. Linked to an Identity user account. Has legal ID, name, contact info, and performance score
- **Application**: A funding request belonging to an applicant. Contains one or more items. Has a state (Draft, Submitted) and version history
- **Item (Line)**: A single product/service within an application. Has product name, category, technical specifications, and an impact definition. Contains supplier quotations
- **Category**: Predefined lookup for classifying items
- **ImpactTemplate**: Administrator-defined template specifying an impact type name and its parameter definitions (name, data type, required/optional, validation constraints)
- **Impact**: An item's structured impact definition. References an ImpactTemplate and stores the user-provided parameter values
- **Supplier**: A vendor providing a quotation. Has legal ID, contact info, location, invoice capability, shipping details, warranty, and compliance status
- **Quotation**: Links a supplier to an item. Includes price, validity period, and a reference to the uploaded document
- **Document**: File metadata (original name, storage path, size, content type, upload timestamp). Physical file stored on local file system
- **SystemConfiguration**: Key-value settings stored in the database for system-wide business rules
- **VersionHistory**: Audit record tracking changes to an application (who, what, when)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An applicant can complete the full application submission flow (create application, add items, attach quotations, define impact, submit) in under 10 minutes for a typical 3-item application
- **SC-002**: 100% of submission validation rules are enforced — no application can reach Submitted state without meeting all business rules
- **SC-003**: Draft applications persist across sessions — all data entered is recoverable after logout and login
- **SC-004**: System configuration changes (e.g., minimum quotation count) take effect on the next submission attempt without requiring restart
- **SC-005**: Impact templates can be created by administrators and are immediately available for applicant selection
- **SC-006**: All uploaded files are retrievable and match the original content (no corruption or data loss)
- **SC-007**: Every user story has passing Playwright e2e tests covering the golden path and key error scenarios
- **SC-008**: Concurrent edit detection works — two users editing the same application are warned of conflicts

## Assumptions

- Users have modern web browsers (latest 2 versions of Chrome, Edge, Firefox)
- Users have stable internet connectivity for the web application
- Local file system has sufficient storage for document uploads during initial deployment
- A single deployment environment is targeted initially (no multi-tenant requirements)
- Performance score on Applicant is a manually-managed field for this spec; calculation logic is deferred to a later spec
- Role-based authorization (admin vs. applicant) uses basic ASP.NET Identity roles — granular permissions are deferred to the review/approval spec
- The platform is used by a moderate number of concurrent users initially (under 100); high-scale optimizations are deferred
