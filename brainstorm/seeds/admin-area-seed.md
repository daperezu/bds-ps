You are a senior product architect and UX strategist specializing in role-based systems (RBAC), admin panels, and secure workflow platforms (fintech, grant systems, internal tools).

Context:
We are evolving a funding platform called “Programa Semilla,” where:
- Applicants submit funding requests
- Reviewers evaluate and approve/reject them
- There is full traceability and communication across the lifecycle

Until now, the system has only had two roles:
- Applicant
- Reviewer

We are now introducing a third role: Admin.

Goal:
Run a structured brainstorm session to define the Admin role as a foundational system component, including:
- Capabilities
- Access control model
- UX implications
- System constraints
- Future scalability (especially for reporting)

This output will serve as a “Context 0” foundation for future iterations (e.g., reporting modules).

Admin Role Definition (Core Requirements):
- Admin can do EVERYTHING a Reviewer can do
- Additionally, Admin can:
  • Create users internally
  • Assign roles (Applicant or Reviewer)
  • Disable users
  • Reset/change user passwords
  • View reports (NOTE: reporting is NOT to be designed yet, just acknowledge access)

Critical System Constraints:
- There must ALWAYS be a default Admin user seeded in the system
- This default Admin:
  • Email: admin@FundingPlatform.com
  • Password: randomly generated (not hardcoded, must be secure)
- This default Admin user:
  • CANNOT be deleted
  • CANNOT be modified (no updates of any kind)
  • MUST be excluded from all queries where users are listed or managed
  • MUST NOT appear in UI lists or admin tables
- The system must enforce these constraints at both:
  • Database/query level
  • Application logic level

What to Produce:

1. Role Definition (Admin)
   - Clear description of responsibilities
   - Differences vs Reviewer
   - System-level importance

2. Access Control Matrix (RBAC)
   - Table or structured list showing:
     • Applicant
     • Reviewer
     • Admin
   - Actions such as:
     • Create application
     • Review application
     • Approve/reject
     • Communicate (back-and-forth)
     • Manage users
     • Assign roles
     • Disable users
     • Reset passwords
     • Access reports (future)
   - Clearly define who can do what

3. Admin UX Considerations
   - Admin panel structure (high-level)
   - User management interface:
     • Listing users (excluding default admin)
     • Role assignment UX
     • Status (active/disabled)
   - Safety UX patterns (confirmation dialogs, audit hints)

4. Default Admin (Seed Strategy)
   - Best practices for seeding this user
   - How to ensure:
     • It always exists
     • It cannot be modified or deleted
   - Security considerations:
     • Random password generation
     • Environment-based secrets

5. Data & Query Constraints
   - How to enforce exclusion of default admin:
     • In database queries
     • In ORM/service layer
   - Safeguards against accidental exposure or modification

6. Risks & Edge Cases
   - What could go wrong if:
     • Admin is exposed in UI
     • Admin is accidentally modifiable
     • Role logic is not strictly enforced
   - Mitigation strategies

7. Future-Proofing
   - How this Admin role supports:
     • Reporting modules (later)
     • Scaling to more roles (e.g., super admin, auditors)
   - Recommendations for extensibility

Instructions:
- Be precise and system-oriented (not generic UX advice)
- Think in terms of real implementation (backend + frontend)
- Prioritize security, integrity, and maintainability
- Keep everything grounded in this funding platform context
