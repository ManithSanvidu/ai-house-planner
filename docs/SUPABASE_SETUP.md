# Supabase PostgreSQL Setup

HousePlanner.API uses Supabase PostgreSQL and Supabase Auth for authentication.

To configure your local environment to connect to the database, follow these steps:

## 1. Create a `.env` file
In the `HousePlanner.API` directory, create a `.env` file (you can copy `.env.example`).
```bash
cd HousePlanner.API
cp .env.example .env
```

## 2. Configure the Connection String
Inside the `.env` file, set the `DATABASE_CONNECTION_STRING` variable to the Supabase connection string. Replace `[PASSWORD]` with the actual database password.
```env
DATABASE_CONNECTION_STRING=postgresql://postgres.cqfelbazvvbeiwwydwm5:[PASSWORD]@aws-0-ap-northeast-2.pooler.supabase.com:6543/postgres
```
*Note: Do not commit `.env` or any real passwords to version control.*

## 3. Running Locally
Before running the application, make sure the environment variable is loaded. If you are using an IDE like Visual Studio or Rider, you can add this variable to your `launchSettings.json` or run configuration.

To run via CLI and pass the environment variable:
```bash
DATABASE_CONNECTION_STRING="your_connection_string" dotnet run
```

## 4. Verify Database Connection
Once the application is running, you can verify the database connectivity by calling the health check endpoint:
```bash
curl -i http://localhost:5000/api/health/database
```
If the connection is successful, you will receive a `200 OK` response:
```json
{
  "status": "Healthy",
  "message": "Successfully connected to the database."
}
```

## Database Migrations
Migrations are managed using Entity Framework Core. To update your database with existing migrations:
```bash
dotnet ef database update
```
*Important: Do not recreate migrations or drop the database as we need to preserve existing data and entities.*
