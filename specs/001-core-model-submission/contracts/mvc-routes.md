# MVC Routes: Core Data Model & Application Submission

**Date**: 2026-04-15

## Authentication Routes (AccountController)

| Method | Route | Action | Auth | Description |
|--------|-------|--------|------|-------------|
| GET | /Account/Register | Register | Anonymous | Registration form |
| POST | /Account/Register | Register | Anonymous | Submit registration |
| GET | /Account/Login | Login | Anonymous | Login form |
| POST | /Account/Login | Login | Anonymous | Submit login |
| POST | /Account/Logout | Logout | Authenticated | Log out |

## Application Routes (ApplicationController)

| Method | Route | Action | Auth | Description |
|--------|-------|--------|------|-------------|
| GET | /Application | Index | Applicant | List applicant's applications |
| GET | /Application/Create | Create | Applicant | New application form |
| POST | /Application/Create | Create | Applicant | Create draft application |
| GET | /Application/{id} | Details | Applicant | View application details |
| GET | /Application/{id}/Edit | Edit | Applicant | Edit draft application |
| POST | /Application/{id}/Submit | Submit | Applicant | Submit application (validates) |

## Item Routes (ItemController)

| Method | Route | Action | Auth | Description |
|--------|-------|--------|------|-------------|
| GET | /Application/{appId}/Item/Add | Add | Applicant | Add item form |
| POST | /Application/{appId}/Item/Add | Add | Applicant | Create item |
| GET | /Application/{appId}/Item/{id}/Edit | Edit | Applicant | Edit item form |
| POST | /Application/{appId}/Item/{id}/Edit | Edit | Applicant | Update item |
| POST | /Application/{appId}/Item/{id}/Delete | Delete | Applicant | Remove item |
| GET | /Application/{appId}/Item/{id}/Impact | Impact | Applicant | Impact definition form |
| POST | /Application/{appId}/Item/{id}/Impact | Impact | Applicant | Save impact definition |

## Supplier & Quotation Routes (SupplierController / QuotationController)

| Method | Route | Action | Auth | Description |
|--------|-------|--------|------|-------------|
| GET | /Application/{appId}/Item/{itemId}/Supplier/Add | Add | Applicant | Add supplier form |
| POST | /Application/{appId}/Item/{itemId}/Supplier/Add | Add | Applicant | Add supplier to item |
| GET | /Application/{appId}/Item/{itemId}/Quotation/Add | Add | Applicant | Upload quotation form |
| POST | /Application/{appId}/Item/{itemId}/Quotation/Add | Add | Applicant | Upload quotation document |
| POST | /Application/{appId}/Item/{itemId}/Quotation/{id}/Replace | Replace | Applicant | Replace quotation document |
| POST | /Application/{appId}/Item/{itemId}/Quotation/{id}/Delete | Delete | Applicant | Remove quotation |

## Admin Routes (AdminController)

| Method | Route | Action | Auth | Description |
|--------|-------|--------|------|-------------|
| GET | /Admin | Index | Admin | Admin dashboard |
| GET | /Admin/Configuration | Configuration | Admin | System configuration list |
| POST | /Admin/Configuration | Configuration | Admin | Update configuration |
| GET | /Admin/ImpactTemplates | ImpactTemplates | Admin | List impact templates |
| GET | /Admin/ImpactTemplates/Create | CreateTemplate | Admin | New template form |
| POST | /Admin/ImpactTemplates/Create | CreateTemplate | Admin | Create template |
| GET | /Admin/ImpactTemplates/{id}/Edit | EditTemplate | Admin | Edit template form |
| POST | /Admin/ImpactTemplates/{id}/Edit | EditTemplate | Admin | Update template |

## Home Route (HomeController)

| Method | Route | Action | Auth | Description |
|--------|-------|--------|------|-------------|
| GET | / | Index | Anonymous | Landing page / redirect |

## Notes

- All Applicant routes verify the application belongs to the authenticated user
- All Admin routes require the "Admin" Identity role
- File uploads use `multipart/form-data` encoding
- POST actions return redirects on success (PRG pattern) or re-render form with errors on failure
- Anti-forgery tokens required on all POST forms
