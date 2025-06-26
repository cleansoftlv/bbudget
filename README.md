# BBudget

BBudget is an open source client for Lunch Money budgeting platform, providing bank conversion services through a modern web application built with Blazor WebAssembly and Azure Functions.

## License

BBudget is dual-licensed:

- **Open Source License**: [GNU Affero General Public License v3.0 (AGPLv3)](LICENSE)
- **Commercial License**: For all use cases not permitted by AGPLv3, a commercial license is available by request to info@cleansoft.lv

This means:
- If you use BBudget under AGPLv3, you must open source your entire application
- If you want to use BBudget in a proprietary/closed-source application in a way not permitted by AGPLv3, you need a commercial license

## Project Overview

BBudget is a comprehensive bank converter application featuring:
- **Frontend**: Blazor WebAssembly with Bootstrap Blazor UI
- **Backend**: Azure Functions (.NET 8.0)
- **Database**: SQL Server with Entity Framework Core
- **Architecture**: Clean architecture with shared libraries
- **Features**: Bank integrations (Revolut), authentication, licensing system

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension
- SQL Server (LocalDB for development)
- Azure Functions Core Tools (for local development)
- Node.js (for build tools)

## Getting Started for Developers

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/bbudget.git
cd bbudget
```

### 2. Configuration Setup

**IMPORTANT**: Before you can run the project, you need to configure several files:

1. **Find all `.sample` and files and create copies without the `.sample` extension**
2. **Find all `_sample` and files and create copies without the `_sample` part**
3. **Replace all `<your...>` placeholders with actual values**

Here are the configuration files you need to set up:

#### Application Configuration Files:

| Sample File | Target File | Description |
|------------|-------------|-------------|
| `LMApp/wwwroot/appsettings.sample.json` | `LMApp/wwwroot/appsettings.json` | Frontend configuration |
| `LMFunctions/appSettings.sample.json` | `LMFunctions/appSettings.json` | Main API configuration |
| `LMFunctions/appSettings.Release.sample.json` | `LMFunctions/appSettings.Release.json` | Production API settings |
| `PublicApi/appSettings.sample.json` | `PublicApi/appSettings.json` | Public API configuration |
| `PublicApi/appSettings.Release.sample.json` | `PublicApi/appSettings.Release.json` | Production public API |
| `Services/servicesSettings.sample.json` | `Services/servicesSettings.json` | Core services configuration |
| `Test/appsettings.sample.json` | `Test/appsettings.json` | Test configuration |
| `Shared/LicensingConstants.sample.cs` | `Shared/LicensingConstants.cs` | Licensing constants |
| `Landing/swa-cli.config.sample.json` | `Landing/swa-cli.config.json` | Static Web Apps CLI config |

#### Database Configuration Files:

| Sample File | Target File | Description |
|------------|-------------|-------------|
| `LMDatabse/Security/user.sample.sql` | `LMDatabse/Security/user.sql` | Database user setup |
| `LMDatabse/Security/RoleMemberships.sample.sql` | `LMDatabse/Security/RoleMemberships.sql` | Database roles |
| `LMDatabse/Compare.sample.scmp` | `LMDatabse/Compare.scmp` | Database comparison settings |

### 3. Configuration Values to Replace

In the configuration files, replace these placeholders:

- `<yourlocaldbconnectionstring>` - Your SQL Server connection string
- `<yourissuer>` - JWT issuer (e.g., "https://localhost:7071")
- `<yoursecret>` - JWT signing secret (generate a secure random string)
- `<yoursecret2>` - Token encryption secret (generate another secure random string)
- `<your application insights connection string>` - Azure Application Insights connection (optional for local dev)
- `<yourrevolutsecret>` - Revolut API secret key
- `<yourrevolutpublickey>` - Revolut API public key
- `<yourredirecturl>` - OAuth redirect URL for Revolut
- `<yourwebhooksecret>` - Webhook verification secret
- `<yourwebhookstorageconnectionstring>` - Azure Storage connection for webhooks
- `<yourwebhookqueuename>` - Queue name for webhook processing

### 4. Database Setup

1. Create a local database using the SQL Server Database Project:
   ```bash
   cd LMDatabse
   # Deploy using Visual Studio or SQL Server Data Tools
   ```

2. Update the connection string in `Services/servicesSettings.json`

### 5. Build and Run

1. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

2. **Build the solution:**
   ```bash
   dotnet build
   ```

3. **Run the Azure Functions locally:**
   ```bash
   cd LMFunctions
   func start
   ```

4. **Run the Blazor WebAssembly app:**
   ```bash
   cd LMApp
   dotnet run
   ```

5. **Run the Public API (in a separate terminal):**
   ```bash
   cd PublicApi
   func start --port 7072
   ```

### 6. Development Tools

- **Static Web Apps CLI**: For testing the full stack locally
  ```bash
  npm install -g @azure/static-web-apps-cli
  swa start
  ```

- **PowerShell Scripts**:
  - `swa.ps1` - Simplified SWA startup script
  - `fix_debug.ps1` - Development debugging utilities

## Project Structure

```
BBudget/
├── LMApp/                    # Blazor WebAssembly frontend
├── LMFunctions/              # Main Azure Functions API
├── PublicApi/                # Public-facing API
├── Services/                 # Business logic layer
├── Shared/                   # Common models and DTOs
├── LMDatabse/               # SQL Server database project
├── FunctionCommon/          # Azure Functions utilities
├── Landing/                 # Marketing landing page
├── Test/                    # Unit and integration tests
└── Migrations/              # Database migrations
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Please ensure:
- Code follows existing style conventions
- New features include appropriate tests
- Documentation is updated as needed

## Support

- **Open Source Support**: GitHub Issues
- **Commercial Support**: info@cleansoft.lv

## Acknowledgments

BBudget integrates with:
- [Lunch Money](https://lunchmoney.app/) - Personal finance and budgeting app
- [Revolut Business API](https://developer.revolut.com/) - Banking services integration

---

Copyright © 2025 CleanSoft. See [LICENSE](LICENSE) for details.