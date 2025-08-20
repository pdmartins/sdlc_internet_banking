# UI/UX Prototype for Internet Banking Application

## 1. Visual Identity & Brand Integration
- **Brand**: Contoso
- **Fonts**: Inter / Roboto / Segoe UI (all screens)
- **Colors**:
  - Primary Blue: #0078D4
  - Light Gray: #F3F3F3
  - Medium Gray: #C8C8C8
  - Text Black: #1A1A1A
  - White: #FFFFFF
  - Action Hover: #005A9E

## 2. Navigation Components
- **Header Bar**:
  - Left: Contoso logo
  - Center: Main links (Home, Products, Solutions, About, Contact)
  - Right: Profile/Login icon
- **Sidebar (Optional)**:
  - Icons + labels: Dashboard, Reports, Settings, Logout

## 3. Registration Flow (Step-by-Step)
### Screen 1: Welcome & Introduction
- Contoso branding, brief onboarding, [ Continuar ] button

### Screen 2: Personal Information
- Inputs: Full Name, Email, Phone
- Label above input, bordered in Light Gray
- Inline validation (error in Medium Gray, success in Primary Blue)
- [ Continuar ] (primary), [ Cancelar ] (secondary)

### Screen 3: Identity Verification
- Upload government ID (drag & drop or button)
- Live selfie capture (camera integration)
- Progress bar for verification
- Alerts for errors (❌ Erro: Documento inválido)

### Screen 4: Security Setup
- Password input (strength meter, show/hide toggle)
- Security questions (select dropdown)
- Enable MFA (checkbox/radio for SMS or Authenticator)
- [ Continuar ]

### Screen 5: Confirmation & Welcome
- Success alert (✔️ Sucesso: Cadastro realizado)
- Brief tutorial card: navigation hints, security tips

## 4. Login Flow (Step-by-Step)
### Screen 1: Login
- Inputs: Email/Username, Password
- Checkbox: "Lembrar dispositivo"
- [ Continuar ]
- [ Esqueci minha senha ] link

### Screen 2: MFA Verification
- Input for OTP code
- "Reenviar código" button (limited attempts)
- Error alert (❌ Erro: Código inválido)

### Screen 3: Password Recovery
- Email input
- Security question challenge (dropdown + input)
- Success/error alerts as needed

### Device Management
- List of registered devices (card/table)
- Option to revoke device

## 5. Dashboard
- **Balance Card**: Prominent, Primary Blue background, large font
- **Transaction History Table**:
  - Columns: Date, Type, Amount, Status
  - Filter dropdowns, export buttons (CSV/PDF)
- **Navigation Cards**: Quick links to Transfer, Pay Bills, View Reports

## 6. Transaction Flow
### Screen 1: Initiate Transaction
- Select: Credit or Debit (radio button)
- Inputs: Recipient, Amount, Description
- Dropdown for account selection

### Screen 2: Review & Confirm
- Card with transaction summary
- Fee calculation
- [ Confirmar ] (primary), [ Cancelar ] (secondary)

### Screen 3: Completion
- Success alert (✔️ Sucesso: Transação realizada)
- Error alert (❌ Erro: Saldo insuficiente)

## 7. Components Reference
- **Inputs, Dropdowns, Checkboxes, Buttons**: Styled per Contoso kit
- **Cards**: Used for dashboard, notifications, tutorials
- **Tables**: Transaction history, device management
- **Alerts**: Inline and modal, with clear icons and colors

## 8. Feedback & Accessibility
- Real-time validation, clear error/success states
- All actions available via keyboard
- Screen reader labels on all components
- Responsive for mobile/tablet/desktop

## 9. Page Structure Example
```
[Header]
-----------------------------
[Sidebar] | [Content Area]
         | - Title
         | - Breadcrumb
         | - Forms / Cards / Tables
         | - Action Buttons
-----------------------------
[Footer: Contact / Terms / Privacy]
```

## 10. Technical Recommendations
- **Prototyping tools**: Figma, Adobe XD (for design handoff)
- **Front-end frameworks**: React + Tailwind CSS (for production)
- **Accessibility**: Full WCAG 2.1 AA compliance
- **Security**: Mask sensitive inputs, never auto-fill passwords
