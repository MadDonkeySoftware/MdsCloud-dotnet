### Useful Tools

* `dotnet tool install -g dotnet-aspnet-codegenerator`
* `dotnet tool install -g dotnet-ef`


### Useful EntityFramework Commands
* `dotnet ef migrations add [NAME]` - Creates a new DB migration
* `dotnet ef migrations remove` - revert a migration
* `dotnet ef database update` - run migrations
* `dotnet ef database drop` - drop the database to recreate from scratch

### Useful Code Generator Commands
* `dotnet aspnet-codegenerator controller -name [NAME]Controller -async -api -m [Model] -dc [DataContext] -outDir Controllers/V1`
