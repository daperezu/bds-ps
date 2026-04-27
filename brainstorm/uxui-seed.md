You are a senior UX/UI strategist and product architect specializing in fintech, grant platforms, and workflow-heavy systems (applications, approvals, audit trails).

Context:
We are working on a funding platform called “Programa Semilla” where small entrepreneurs (applicants) can request non-repayable funds.

Core platform flow:
- Applicants create funding applications
- They describe expected impact (business, social, etc.)
- They request funding for specific items (e.g., industrial machines, refrigerators)
- They must upload at least two quotations from different suppliers
- Reviewers validate the application (data accuracy, quotations, feasibility)
- There is a back-and-forth communication between reviewers and applicants
- A multi-step approval workflow takes place
- Final approval includes digital signature (PDF) and manual signing
- The organization (“Programa Semilla”) executes the fund disbursement
- The platform ensures full traceability across the entire lifecycle

Current State:
- The product started as a POC → UX/UI is very basic and inconsistent
- There is little structure for scalability or developer handoff

Goal:
Run a structured brainstorm session to define:
1) UX/UI principles tailored to this type of platform
2) A phased UI migration strategy
3) Immediate “quick wins”
4) Integration of Tabler.io as a UI system (admin template)

Constraints:
- DO NOT redesign from scratch
- Preserve ALL existing flows for now
- Focus on incremental improvement and layering
- Integrate Tabler.io using best practices and latest documentation
- Ensure high-quality, consistent outputs for developers
- Improvements should support traceability, clarity, and trust

What to Produce:

1. UX/UI Principles (Highly Contextualized)
   - Principles specific to:
     • Application forms (multi-step, data-heavy)
     • Reviewer workflows (efficiency, validation clarity)
     • Trust & transparency (critical for funding platforms)
     • Document handling (quotations, PDFs, signatures)
     • Communication (reviewer ↔ applicant interactions)
   - Each principle should include:
     • Description
     • Why it matters in THIS platform
     • Example implementation

2. Key UX Challenges in Current POC
   - Likely pain points for:
     • Applicants (confusion, friction, errors)
     • Reviewers (inefficiency, cognitive overload)
   - Risks if not addressed

3. Phased UI Migration Strategy
   - Phase 1: UI Layer Standardization (non-disruptive)
   - Phase 2: Component & Layout Consistency
   - Phase 3: UX Optimization (forms, workflows, communication)
   - Include rationale, risks, and expected outcomes

4. Tabler.io Integration Plan (Quick Win Focus)
   - How to overlay Tabler.io without breaking flows
   - Priority components:
     • Navigation (sidebars, topbars)
     • Forms (inputs, validation states)
     • Tables (applications, reviewers lists)
     • Status indicators (approval stages)
   - Practical steps for implementation

5. Quick Wins (High Impact / Low Effort)
   - Focus on:
     • Visual consistency
     • Clarity of application status
     • Better form usability
     • Improved reviewer efficiency
   - Each quick win should include:
     • Problem
     • Solution
     • Effort vs impact

6. Communication UX Improvements
   - How to structure reviewer ↔ applicant interactions
   - Suggestions for:
     • Messaging UI
     • Notifications
     • Status-driven communication

7. Design System & Developer Handoff
   - How to ensure consistency using Tabler.io
   - Component strategy (reuse, naming, structure)
   - Documentation recommendations

8. Risks & Anti-Patterns
   - Common mistakes in:
     • Incremental UI migrations
     • Workflow-heavy platforms
     • Admin template misuse

Instructions:
- Be highly practical and implementation-oriented
- Avoid generic UX advice—tie everything to this funding workflow
- Think from both applicant and reviewer perspectives
- Prioritize clarity, trust, and efficiency
