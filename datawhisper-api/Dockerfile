# Use the official .NET 10 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["DataWhisper.API.csproj", "./"]
RUN dotnet restore "./DataWhisper.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/."
RUN dotnet build "DataWhisper.API.csproj" -c Release -o /app/build

# Build the runtime image
FROM build AS publish
RUN dotnet publish "DataWhisper.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DataWhisper.API.dll"]