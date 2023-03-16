FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "source/MdsCloud.DbTooling/MdsCloud.DbTooling.csproj"
WORKDIR "/src/source/MdsCloud.DbTooling"
RUN dotnet build "MdsCloud.DbTooling.csproj" --no-incremental -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MdsCloud.DbTooling.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
CMD ["dotnet", "MdsCloud.DbTooling.dll", "migrations", "run", "Identity"]
