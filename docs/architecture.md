# ARCHITECTURE SPECIFICATION – INTERNET BANKING PROTOTYPE (CONTOSO STANDARD)

## 1. Overview
**Objective:**  Deliver a secure, scalable, and maintainable internet banking application, fully aligned with Contoso's UI/UX kit, PRD, and Microsoft architecture best practices.

**Tech Stack:**
- **Frontend:** React.js (TypeScript)
- **Backend:** ASP.NET Core Web API (C#)
- **Authentication:** Microsoft Identity Platform (Azure AD, OAuth 2.0/OpenID Connect)
- **Database:** Azure SQL Server
- **Hosting:** Azure App Service (Web + API)
- **CI/CD:** Azure DevOps or GitHub Actions
- **Monorepo:** Nx or Yarn Workspaces

## 2. Technology Stack
- React.js + TypeScript, ASP.NET Core Web API 
- Azure AD (OAuth 2.0 + OIDC), Azure SQL, Azure App Service, shared DTO contracts, CI/CD via Azure DevOps/GitHub Actions.

## 3. Solution Structure
- Monorepo: /client (React), /api (ASP.NET Core), /shared (DTOs/types)
- Frontend: Azure AD auth (MSAL.js), secure routing, Contoso UI Kit, React Context/Zustand, React Hook Form + Yup, HTTPS API, error handling, global alerts
- Backend: Clean Architecture (Domain, Application, Infrastructure, Web), DI, EF Core, Azure SQL, external KYC, controllers, auth middleware, logging, config via appsettings.{env}.json

## 4. Security Architecture
- Azure AD auth, secure tokens, HTTPS, CORS, anti-CSRF, XSS protection, SQL injection mitigation, secrets in Azure Key Vault, audit logging.

## 5. Development Patterns
- UI via Contoso Kit, responsive layouts, accessibility; backend separation of concerns, DTOs/validation, unit/integration tests, environment configs, logging/monitoring hooks.

## 6. CI/CD Pipeline
- Lint/tests, build, deploy, post-deploy tests, rollback; 
- tools: Azure DevOps/GitHub Actions; artifacts: builds, reports, logs.

## 7. Deployment Topology
- Azure App Service (frontend/backend), Azure SQL (encrypted/geo-redundant), Azure Key Vault, Azure Monitor, optional WAF/DDOS for prod.

## 8. Data Flow Example
- Frontend HTTPS → API for registration/login → Azure AD/KYC → session token → transaction flows → history retrieval.

## 9. Testing & QA
- xUnit (backend), Jest (frontend), integration tests, SonarQube/OWASP ZAP, accessibility (axe-core/Lighthouse), performance (Azure Load Testing/NBomber).

## 10. Scalability & Maintainability
- App Service auto-scaling, Clean Architecture, shared contracts, modular codebase, Application Insights/Monitor, OpenAPI/Swagger, Storybook.

## 11. Compliance & Regulatory
- GDPR, PCI-DSS, SOC2, KYC/AML, Azure geo-location.

## 12. Next Steps
- Monorepo setup, authentication/onboarding, transaction/history modules, CI/CD/Azure setup, stakeholder review/iteration.
