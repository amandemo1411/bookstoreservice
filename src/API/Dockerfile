FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore
COPY ["src/API/BookStore.API.csproj", "src/API/"]
COPY ["src/BusinessLogic/BookStore.BusinessLogic/BookStore.BusinessLogic.csproj", "src/BusinessLogic/BookStore.BusinessLogic/"]
COPY ["src/DataAccess/BookStore.DataAccess/BookStore.DataAccess.csproj", "src/DataAccess/BookStore.DataAccess/"]
COPY ["src/Models/BookStore.Models/BookStore.Models.csproj", "src/Models/BookStore.Models/"]
COPY ["src/Utilities/BookStore.Utilities/BookStore.Utilities.csproj", "src/Utilities/BookStore.Utilities/"]
RUN dotnet restore "src/API/BookStore.API.csproj"

# Copy the rest of the source and publish
COPY . .
WORKDIR "/src/src/API"
RUN dotnet publish "BookStore.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BookStore.API.dll"]

