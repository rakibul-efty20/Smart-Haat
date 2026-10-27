<div align="center">
  <img src="frontend/smarthaat-web/public/assets/smart-haat-brand.png" alt="Smart Haat logo" width="360" />

  # Smart Haat

  **A modern grocery marketplace and store operations platform**

  Browse everyday essentials, manage stock and orders, and run in-store sales from one connected application.
</div>

---

## About

Smart Haat is a full-stack grocery commerce project built with an ASP.NET Core Web API and an Angular storefront and operations portal. The API owns pricing, inventory, authentication, and order rules; the browser is a client of those services.

The repository is organized as a .NET modular monolith with separate Domain, Application, Infrastructure, and API projects, alongside the Angular frontend.

## What it includes

- **Storefront:** category browsing, product search and details, guest/customer carts, checkout, and customer order history.
- **Identity and access:** ASP.NET Core Identity, JWT access tokens, rotating refresh tokens in secure HttpOnly cookies, and Customer, Salesman, and Admin roles.
- **Catalog management:** hierarchical categories, product SKUs, activation controls, and admin image uploads.
- **Inventory:** batch receiving, stock adjustments, expiry tracking, stock movements, low-stock and expiry reports, and FEFO consumption for stock reductions.
- **Orders and checkout:** server-calculated prices, cash-on-delivery checkout, stock reservations, order lifecycle operations, and a Salesman POS workflow.
- **Store configuration:** delivery zones, coupons, and sales reports.
- **API exploration:** OpenAPI and Scalar in Development, plus a health endpoint.

## Technology

| Area | Stack |
|---|---|
| API | ASP.NET Core Web API, .NET 10 |
| Architecture | Domain / Application / Infrastructure / API |
| Data | Entity Framework Core 10, SQL Server |
| Authentication | ASP.NET Core Identity, JWT bearer tokens |
| API reference | OpenAPI, Scalar |
| Frontend | Angular 22, TypeScript |

## Repository layout

```text
.
├── src/
│   ├── SmartHaat.Api/             # HTTP API, authentication, OpenAPI and hosting
│   ├── SmartHaat.Application/     # Use cases and application contracts
│   ├── SmartHaat.Domain/          # Core business entities and rules
│   └── SmartHaat.Infrastructure/  # EF Core, SQL Server and service implementations
├── tests/
│   └── SmartHaat.UnitTests/       # .NET test project
├── frontend/
│   └── smarthaat-web/             # Angular storefront and staff portal
└── SmartHaat.slnx                 # .NET solution
```

## Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB, Developer, Express, or another SQL Server instance)
- Node.js 24.15+ in the Node 24 line and npm 11+ for the Angular frontend

## Run the API

### 1. Configure local secrets

The API expects a JWT signing key of at least 32 bytes. The Development admin-bootstrap endpoint also needs a configured email and bootstrap key. Set these with .NET User Secrets from the API project directory; do not commit real credentials or keys.

```powershell
cd src/SmartHaat.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "<random-secret-at-least-32-bytes>"
dotnet user-secrets set "AdminBootstrap:Email" "admin@example.test"
dotnet user-secrets set "AdminBootstrap:Key" "<separate-random-bootstrap-secret>"
```

Development configuration uses SQL Server at `Server=.;Database=SmartHaat;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`. To use another SQL Server connection string, configure it as a user secret:

```powershell
dotnet user-secrets set "ConnectionStrings:SmartHaat" "<your-sql-server-connection-string>"
```

### 2. Restore, migrate, and start

From the repository root:

```powershell
dotnet restore SmartHaat.slnx
dotnet tool restore
dotnet ef database update --project src/SmartHaat.Infrastructure --startup-project src/SmartHaat.Api
dotnet run --project src/SmartHaat.Api --launch-profile https
```

The Development profile serves the API at `https://localhost:7061` and `http://localhost:5251`. On first startup, the API applies pending migrations and creates the standard roles in Development. The explicit `database update` step is useful before starting so migration errors are reported directly.

If your local HTTPS certificate is not trusted, run `dotnet dev-certs https --trust` and restart the browser/API.

### 3. Open Scalar and health check

- Scalar: <https://localhost:7061/scalar>
- OpenAPI document: <https://localhost:7061/openapi/v1.json>
- Health: <https://localhost:7061/health>

OpenAPI and Scalar are only enabled in the Development environment.

### 4. Configure and create the first development administrator

Keep the bootstrap key and administrator password out of this README and source control. Configure the bootstrap email and key locally from `src/SmartHaat.Api` using User Secrets:

```powershell
cd src/SmartHaat.Api
dotnet user-secrets set "AdminBootstrap:Email" "<your-admin-email>"
dotnet user-secrets set "AdminBootstrap:Key" "<a-separate-random-bootstrap-key>"
```

In Scalar, call `POST /api/development/admin-bootstrap` once. Send the configured key in the `X-Bootstrap-Key` header and a JSON body with the display name and the **administrator login password you choose**:

```json
{
  "displayName": "Store Administrator",
  "password": "<strong-password-at-least-10-characters>"
}
```

These values have different purposes:

- `AdminBootstrap:Email` is the email address of the initial Admin account.
- `AdminBootstrap:Key` is only the secret header for the Development bootstrap request. It is **not** the Admin login password.
- The `password` in the bootstrap request body becomes the Admin login password. It is not stored in `AdminBootstrap` configuration.

After the bootstrap request succeeds, sign in with the configured email and the password you used in its request body at `POST /api/auth/login`. The response includes a short-lived access token. Use it as a Bearer token for protected API calls. The bootstrap endpoint is Development-only and refuses to create another Admin after one already exists.

Customer accounts can be created through `POST /api/auth/register`. Registration assigns the Customer role; it does not create staff accounts.

## Run the frontend

In a second terminal:

```powershell
cd frontend/smarthaat-web
npm ci
npm start
```

Open <http://localhost:4200>. The frontend's default API base URL is `https://localhost:7061`. The Development API allows the default Angular origins on port 4200. Start the API using its HTTPS profile for refresh-cookie authentication to work correctly.

Build the frontend for production with:

```powershell
npm run build
```

## Common API areas

| Area | Example routes | Typical access |
|---|---|---|
| Authentication | `/api/auth/register`, `/api/auth/login`, `/api/auth/refresh`, `/api/auth/me` | Public or authenticated |
| Categories and products | `/api/categories`, `/api/products` | Read public; writes Admin |
| Cart and customer orders | `/api/cart`, `/api/orders` | Guest and/or Customer, depending on operation |
| Inventory | `/api/admin/inventory` | Admin or Salesman to view; Admin for stock writes |
| Staff order management | `/api/admin/orders` | Admin or Salesman according to operation |
| POS | `/api/pos/orders` | Salesman |
| Delivery zones and coupons | `/api/delivery-zones`, `/api/coupons`; admin routes under `/api/admin/...` | Read/validation public; management Admin |
| Reports | `/api/admin/reports/sales` | Admin |

Use Scalar's OpenAPI operations for exact request DTOs, validation rules, and response shapes; these can evolve as the API is extended. Protected calls require `Authorization: Bearer <access-token>`. Browser clients must include credentials for refresh-cookie and guest-cart operations.

## Development commands

```powershell
# Build the .NET solution
dotnet build SmartHaat.slnx

# Run .NET tests
dotnet test SmartHaat.slnx

# Build the Angular frontend (run inside frontend/smarthaat-web)
npm run build
```

## Current scope

The current application supports the workflows listed above. Features such as saved customer addresses, payment-provider integration, automated email/SMS notifications, report exports, and full production deployment configuration are not included yet. SQL Server, mail delivery, and production hosting must be configured for the target environment before deployment.

## Security notes

- Keep JWT signing keys, bootstrap keys, passwords, and production connection strings out of source control.
- Store local development values with User Secrets or environment variables; use an appropriate secret manager in production.
- The Development bootstrap endpoint must not be exposed as a production admin-provisioning mechanism.
- Configure allowed frontend origins and HTTPS for the environment where the API is deployed.

## License

No license has been added yet. All rights are reserved unless a license is added to this repository.

