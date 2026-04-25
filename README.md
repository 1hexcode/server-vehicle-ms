## Dev workflow — next time you change a model

Whenever you add, edit, or delete an entity in `Models/Entities/` (or change anything in `Data/Configurations/`), regenerate the migration and apply it to your local database:

```bash
# from the repo root
cd server_vehicle_parts_ms

# 1. create a new migration — name it after WHAT changed, in PascalCase
dotnet ef migrations add AddCustomerAddressColumn

# 2. apply pending migrations to the local DB
dotnet ef database update
```

### Naming the migration
Use a short, descriptive PascalCase name that reads as "what this migration does":

| Change you made                              | Good migration name                  |
| -------------------------------------------- | ------------------------------------ |
| Added new entity `Vendor`                    | `AddVendorTable`                     |
| Added a column to an existing entity         | `AddPhoneToCustomer`                 |
| Removed a column                             | `RemoveLoyaltyPointsFromUser`        |
| Renamed a column                             | `RenameNameToFullNameOnUser`         |
| Changed a relationship                       | `MakeVehicleIdNullableOnInvoice`     |
| Added an index                               | `IndexUserPhone`                     |

Avoid `UpdateSomething`, `Fix`, `Test`, etc. — the name lives forever in `Data/Migrations/`.

### Common follow-ups

```bash
# preview the SQL the migration will generate (without touching the DB)
dotnet ef migrations script

# undo the last migration BEFORE it has been applied
dotnet ef migrations remove

# roll the DB back to a specific migration (after it was applied)
dotnet ef database update <PreviousMigrationName>

# wipe the local DB and replay everything (dev only — destroys data)
dotnet ef database drop --force
dotnet ef database update
```

### Before you commit
- Check `Data/Migrations/` — the new `<Timestamp>_<Name>.cs` and the updated `AppDbContextModelSnapshot.cs` should both be staged.
- Run `dotnet build` to make sure the migration compiles.
- Don't edit an old migration that's already been pushed to the shared branch — create a new one instead.
