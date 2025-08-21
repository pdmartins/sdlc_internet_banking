# Quick Start Guide - Contoso Bank Backend API

## 🎯 **Issue Resolved!**

The build errors have been fixed by removing the conflicting interface file that contained old method signatures.

## ✅ **Current Status**
- ✅ **Build**: Successful (all projects compile)
- ✅ **Architecture**: Clean Architecture implemented
- ✅ **Database**: Migration created and ready
- ✅ **API**: Registration endpoints implemented
- ✅ **Integration**: Ready for frontend connection

## 🚀 **Running the Backend**

### 1. **Build the Solution**
```bash
cd d:\Repos\_av\Avanade\sdlc_internet_banking\api
dotnet build
```

### 2. **Update Database** (First time only)
```bash
cd src\ContosoBank.Web
dotnet ef database update --project ..\ContosoBank.Infrastructure
```

### 3. **Run the API Server**
```bash
dotnet run --urls "https://localhost:7000;http://localhost:5000"
```

### 4. **Access Swagger Documentation**
Open in browser: `https://localhost:7000`

## 📡 **API Endpoints Available**

### **Registration Flow**
- `POST /api/registration/register` - Register new user
- `POST /api/registration/{userId}/security` - Setup security
- `GET /api/registration/validate/email` - Email validation
- `GET /api/registration/validate/cpf` - CPF validation  
- `GET /api/registration/validate/phone` - Phone validation

### **Health Check**
- `GET /health` - API health status

## 🔗 **Frontend Integration**

### **Update React Frontend to Use API**

1. **Update RegistrationContext.tsx**:
```typescript
// Replace memory storage with API calls
const registerUser = async (personalInfo: PersonalInfo) => {
  const response = await fetch('https://localhost:7000/api/registration/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(personalInfo)
  });
  const result = await response.json();
  setRegistrationData(prev => ({ ...prev, userId: result.userId, account: result.account }));
};
```

2. **Update SecuritySetupForm.tsx**:
```typescript
// Call security setup API
const setupSecurity = async (securityInfo: SecurityInfo) => {
  const response = await fetch(`https://localhost:7000/api/registration/${userId}/security`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(securityInfo)
  });
  const result = await response.json();
  // Handle success
};
```

3. **Add Real-time Validation**:
```typescript
// Email validation
const checkEmail = async (email: string) => {
  const response = await fetch(`https://localhost:7000/api/registration/validate/email?email=${email}`);
  const result = await response.json();
  return result.available;
};
```

## 🗃️ **Database Schema**

The migration creates these tables:
- **Users** - Personal info, credentials, security settings
- **Accounts** - Bank account details, balances, limits  
- **Transactions** - Transaction history and audit trail
- **SecurityEvents** - Security audit logging

## 🔐 **Security Features**

- ✅ Password hashing (SHA-256 + salt)
- ✅ Input validation and sanitization
- ✅ CPF validation with check digits
- ✅ Uniqueness constraints (email, CPF, phone)
- ✅ Security event logging
- ✅ CORS protection
- ✅ Security headers

## 📊 **Testing the API**

### **Sample Registration Request**:
```json
POST /api/registration/register
{
  "fullName": "João da Silva",
  "email": "joao@email.com", 
  "phone": "(11) 99999-9999",
  "cpf": "123.456.789-01",
  "dateOfBirth": "1990-01-01"
}
```

### **Sample Security Setup Request**:
```json
POST /api/registration/{userId}/security
{
  "password": "MinhaSenh@123",
  "securityQuestion": "Nome da sua mãe?",
  "securityAnswer": "Maria",
  "mfaOption": "SMS",
  "passwordStrength": "Strong"
}
```

## 🎉 **Next Steps**

1. **Start Backend**: Run the API server
2. **Update Frontend**: Replace context with API calls  
3. **Test Integration**: Complete registration flow end-to-end
4. **Add Features**: Authentication, transactions, etc.

The backend is now fully functional and ready for integration with your React frontend!
