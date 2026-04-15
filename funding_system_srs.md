# Software Requirements Specification (SRS)
## System: Funding Request & Evaluation Platform

---

## 1. Overview
This system is designed to streamline the submission, evaluation, approval, and tracking of non-reimbursable funding requests for entrepreneurs and incubators. It replaces manual Excel-based processes with a structured, auditable workflow.

---

## 2. Actors

### 2.1 Applicant (Entrepreneur)
- Submits funding requests
- Provides product details, suppliers, and justifications
- Reviews results and accepts/rejects decisions
- Signs documents digitally
- Uploads corrections or appeals

### 2.2 Internal Reviewer (Staff)
- Reviews submitted applications
- Validates suppliers and technical requirements
- Approves/rejects each item (line)
- Selects supplier
- Defines rejection reasons
- Manages payment and final documentation

### 2.3 System
- Stores and processes all data
- Automates validations where possible
- Generates reports and documents
- Sends notifications
- Maintains audit trail

---

## 3. Core Entities

- Application
- Applicant
- Item (Line)
- Category
- Impact
- Supplier
- Supplier Evaluation
- Quotation
- Resolution
- Appeal
- Document (PDF, invoice, receipt, photo)
- Payment
- Version History

---

## 4. Workflow

### 4.1 Application Submission
1. Applicant creates a request
2. Adds one or multiple items
3. For each item:
   - Defines product (free text)
   - Selects category
   - Adds technical specifications
   - Defines impact (guided form)
   - Adds suppliers (min configurable: 2–3)
   - Uploads quotations per supplier

4. Submits application

---

### 4.2 Review Process
1. Reviewer receives notification
2. Reviews:
   - Applicant performance score
   - Technical equivalence of quotations
   - Supplier compliance
   - Impact validity

3. For each item:
   - Approve / Reject / Request more info
   - Provide rejection reason
   - Select supplier (system recommends)

---

### 4.3 Applicant Response
- Applicant views results
- Can:
  - Accept
  - Reject
  - Appeal
  - Modify request

---

### 4.4 Final Approval & Signature
- System generates PDF document
- Applicant downloads, signs digitally, and uploads
- Reviewer validates receipt

---

### 4.5 Payment & Closure
- Record:
  - Payment account
  - Transaction number
  - Upload receipt
  - Upload invoice
  - Upload delivery evidence (photos)

---

## 5. Business Rules

- Each item must have multiple supplier quotations (configurable minimum)
- Quotations must be technically equivalent
- If not equivalent → automatic rejection
- Impact must be structured (not free text)
- Applicant must meet minimum performance threshold
- Approval is manual and per item
- Rejection must include a reason
- Version history must be maintained
- Final approval requires signed PDF document
- All steps must be traceable

---

## 6. Supplier Evaluation Criteria

- Electronic invoice availability
- Shipping cost or inclusion
- Warranty duration
- Quotation validity
- Price
- Compliance with:
  - Tax authority (Hacienda)
  - Social security (Caja)
  - Public procurement system (SICOP)
- Tie-breaker: lowest price

---

## 7. Data Requirements

### Item
- Name (free text)
- Category
- Technical specs
- Impact (type, % increase, timeframe)

### Supplier
- Legal ID
- Contact info
- Location
- Invoice capability
- Shipping details
- Warranty
- Compliance status

### Documents
- Quotations
- Screenshots (manual validations)
- Signed PDF
- Invoice
- Payment receipt
- Photos

---

## 8. Documents

System must generate and manage:
- Application summary
- Approval/rejection resolution
- Legal declaration (PDF)
- Signed agreements
- Payment evidence bundle

---

## 9. States

### Application
- Draft
- Submitted
- Under Review
- Resolved
- Closed

### Item
- Pending
- Approved
- Rejected
- Needs Info

### Supplier
- Pending Validation
- Valid
- Invalid

---

## 10. Notifications

- Submission received
- Review required
- Resolution available
- Signature pending
- Signature received

---

## 11. Reporting

- Export full application history
- Export documents bundle per applicant
- Include:
  - Approved/rejected items
  - Appeals
  - All documents

---

## 12. Non-Functional Requirements

- Secure document storage
- Audit-ready traceability
- Scalable for large volume of applications
- Configurable rules and thresholds
- Support for manual and automated validation

---

## 13. Future Considerations

- Integration as module within Mentory platform
- Automation of external validations (APIs)
- Cold storage for long-term document retention

