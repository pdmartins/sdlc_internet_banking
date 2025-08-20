# EPICS, FEATURES, AND USER STORIES – INTERNET BANKING PROJECT (CONTOSO STANDARD)

## EPIC 1: Secure User Registration & Onboarding
### Feature 1.1: Registration Flow
- **User Story 1.1.1:**  *As a new user, I want to access a welcome screen with Contoso branding and information about the app, so I feel confident to proceed with registration.*
  - **Acceptance Criteria:** Welcome screen displays logo, description, and [Continuar] button. Navigation to registration form is intuitive.
- **User Story 1.1.2:**  *As a new user, I want to enter my personal information (full name, email, phone) with real-time validation, so I can register accurately and quickly.*
  - **Acceptance Criteria:** All fields required; format validation; inline error/success feedback; [Continuar] and [Cancelar] buttons.
- **User Story 1.1.3:**  *As a new user, I want to set a strong password and configure multi-factor authentication (MFA), so my account is protected from unauthorized access.*
  - **Acceptance Criteria:** Password strength meter; security questions; MFA options (SMS/Auth app); backend validation; error handling.
- **User Story 1.1.4:**  *As a new user, I want to see a confirmation and onboarding tutorial after successful registration, so I know how to use the app securely.*
  - **Acceptance Criteria:** Success alert; masked account summary; tutorial card with navigation/security tips.

### Feature 1.2: Identity Verification & Security
- **User Story 1.2.1:**  *As the system, I need to validate user age (18+), uniqueness of email/phone, and compliance with GDPR.*
  - **Acceptance Criteria:** Backend checks; encrypted PII; rate-limited registration attempts; privacy policy consent prompt.

## EPIC 2: Secure Authentication & Device Management
### Feature 2.1: Login Flow
- **User Story 2.1.1:**  *As a registered user, I want to log in using my email/username and password, so I can access my account securely.*
  - **Acceptance Criteria:** Form with validation; option to remember device; error feedback for failed attempts.
- **User Story 2.1.2:**  *As a user, I want to complete MFA verification after login, so my account is protected against unauthorized access.*
  - **Acceptance Criteria:** Prompt for OTP via SMS/email/Auth app; resend code with limit; error handling for invalid/expired codes.
- **User Story 2.1.3:**  *As a user, I want to recover my password using email and security questions, so I can regain access if I forget my credentials.*
  - **Acceptance Criteria:** Password recovery flow; email verification; security question challenge; reset link expiry; success/error alerts.

### Feature 2.2: Device Registration & Management
- **User Story 2.2.1:**  *As a user, I want to register my device on first login and view/revoke devices from my profile, so I can control account access.*
  - **Acceptance Criteria:** Device fingerprinting; additional verification on new devices; list/revoke devices; audit logging.

### Feature 2.3: Session & Fraud Prevention
- **User Story 2.3.1:**  *As a user, I want automatic logout after inactivity and the option to log out all devices, so my account remains secure.*
  - **Acceptance Criteria:** Configurable inactivity timeout; single-session enforcement; "Logout all devices" feature.
- **User Story 2.3.2:**  *As the system, I want to detect and respond to anomalous login activity, so I can prevent fraud.*
  - **Acceptance Criteria:** Unusual location/device/time triggers step-up authentication or account lock; audit log of login attempts.

## EPIC 3: Transaction Management
### Feature 3.1: Transaction Initiation & Processing
- **User Story 3.1.1:**  *As a user, I want to initiate credit (deposit/incoming transfer) or debit (withdrawal/outgoing transfer) transactions, so I can manage my finances.*
  - **Acceptance Criteria:** Transaction type selection; recipient/account details; amount/description input; backend validation.
- **User Story 3.1.2:**  *As a user, I want to review and confirm transaction details before processing, so I can avoid mistakes.*
  - **Acceptance Criteria:** Transaction summary card; fee display; cancel/edit options.
- **User Story 3.1.3:**  *As a user, I want to receive real-time feedback and updated balance after each transaction, so I can track my financial status.*
  - **Acceptance Criteria:** Success/failure alerts; instant balance update; transaction added to history.

### Feature 3.2: Transaction History & Export
- **User Story 3.2.1:**  *As a user, I want to view, filter, and export my transaction history, so I can analyze and share my financial data.*
  - **Acceptance Criteria:** Paginated/filterable table; export (PDF/CSV); detailed transaction view; audit trail.

### Feature 3.3: Security & Compliance
- **User Story 3.3.1:**  *As the system, I want to monitor all transactions for fraud and enforce limits/fees, so the bank remains compliant and secure.*
  - **Acceptance Criteria:** Fraud pattern detection; velocity/rate limits; transparent fee calculation; PCI-DSS compliance.

## EPIC 4: Dashboard & Account Management
### Feature 4.1: Account Overview
- **User Story 4.1.1:**  *As a user, I want to view my account balance, quick links, and recent transactions on the dashboard, so I have a clear overview of my finances.*
  - **Acceptance Criteria:** Balance card; quick action cards; transaction history table.

### Feature 4.2: Profile & Settings
- **User Story 4.2.1:**  *As a user, I want to update my profile information and configure security settings, so my account details are accurate and secure.*
  - **Acceptance Criteria:** Editable profile form; MFA/device management; password/security question update.

## EPIC 5: Non-Functional, Security, and Compliance
### Feature 5.1: Accessibility & Performance
- **User Story 5.1.1:**  *As a user, I want the app to be accessible and responsive across all devices, so I can use it comfortably at any time.*
  - **Acceptance Criteria:** WCAG 2.1 AA compliance; mobile/tablet/desktop support; <200ms core flow response time.

### Feature 5.2: Security Controls
- **User Story 5.2.1:**  *As the system, I want to enforce HTTPS, secure authentication, and protect against CSRF, XSS, and SQL injection, so user data is always safe.*
  - **Acceptance Criteria:** HTTPS enforced; Azure AD OAuth 2.0; anti-CSRF tokens; React escaping/sanitization; EF Core parameterized queries; Key Vault for secrets.

### Feature 5.3: Monitoring & Logging
- **User Story 5.3.1:**  *As the system owner, I want to monitor, log, and alert on critical events, so I can ensure uptime and compliance.*
  - **Acceptance Criteria:** Azure Monitor/App Insights active; audit logs for all sensitive actions; alerting for failures/anomalies.

### Feature 5.4: CI/CD & Environment Management
- **User Story 5.4.1:**  *As a developer, I want automated pipelines for linting, testing, building, and deploying, so releases are fast and reliable.*
  - **Acceptance Criteria:** Azure DevOps/GitHub Actions pipeline; build/test/deploy steps; environment config via appsettings/env files; post-deploy tests.
