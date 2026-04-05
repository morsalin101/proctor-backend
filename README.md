# PROCTOR Backend

A robust, scalable backend built with **.NET 8/9** following **Clean Architecture** principles. This project serves as the core engine for the PROCTOR system, utilizing PostgreSQL for data persistence and Entity Framework Core for ORM.

## 🏗 Architecture Overview

The solution is divided into four main layers:

* **PROCTOR.Domain**: Core entities, value objects, and business rules (No dependencies).
* **PROCTOR.Application**: Business logic, interfaces, DTOs, and mapping.
* **PROCTOR.Infrastructure**: Data access (EF Core), PostgreSQL configuration, and external services.
* **PROCTOR.API**: Web API controllers, Middleware, and Dependency Injection entry point.

---

## 🚀 Getting Started

### Prerequisites
* [.NET SDK](https://dotnet.microsoft.com/download) (Version 8.0 or later)
* [PostgreSQL](https://www.postgresql.org/download/)
* An IDE (VS Code, Visual Studio, or JetBrains Rider)

### Installation & Setup

1.  **Clone the repository**
    ```bash
    git clone <your-repo-url>
    cd proctor-backend
    ```

2.  **Restore dependencies**
    ```bash
    dotnet restore
    ```

3.  **Configure the Database**
    Update the `ConnectionStrings` in `src/PROCTOR.API/appsettings.json`:
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Host=localhost;Database=ProctorDb;Username=postgres;Password=your_password"
    }
    ```

4.  **Apply Migrations**
    Run the following command from the root directory to create the database schema:
    ```bash
    dotnet ef database update --project src/PROCTOR.Infrastructure --startup-project src/PROCTOR.API
    ```

5.  **Run the Application**
    ```bash
    dotnet run --project src/PROCTOR.API
    ```
    The API should now be running at `http://localhost:5000` (or the port specified in your launch settings).

---

## 🛠 Useful Commands

### Adding a New Migration
If you make changes to the entities in the Domain layer:
```bash
dotnet ef migrations add NameOfYourMigration --project src/PROCTOR.Infrastructure --startup-project src/PROCTOR.API
