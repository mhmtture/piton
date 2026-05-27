FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/QuoteManagement.Api/QuoteManagement.Api.csproj", "src/QuoteManagement.Api/"]
COPY ["src/QuoteManagement.Application/QuoteManagement.Application.csproj", "src/QuoteManagement.Application/"]
COPY ["src/QuoteManagement.Domain/QuoteManagement.Domain.csproj", "src/QuoteManagement.Domain/"]
COPY ["src/QuoteManagement.Infrastructure/QuoteManagement.Infrastructure.csproj", "src/QuoteManagement.Infrastructure/"]
RUN dotnet restore "src/QuoteManagement.Api/QuoteManagement.Api.csproj"
COPY . .
WORKDIR "/src/src/QuoteManagement.Api"
RUN dotnet build "QuoteManagement.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "QuoteManagement.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QuoteManagement.Api.dll"]
