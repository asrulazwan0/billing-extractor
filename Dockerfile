FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BillingExtractor.Api/BillingExtractor.Api.csproj", "BillingExtractor.Api/"]
COPY ["BillingExtractor.Application/BillingExtractor.Application.csproj", "BillingExtractor.Application/"]
COPY ["BillingExtractor.Domain/BillingExtractor.Domain.csproj", "BillingExtractor.Domain/"]
COPY ["BillingExtractor.Infrastructure/BillingExtractor.Infrastructure.csproj", "BillingExtractor.Infrastructure/"]

RUN dotnet restore "BillingExtractor.Api/BillingExtractor.Api.csproj"
COPY . .
WORKDIR "/src/BillingExtractor.Api"
RUN dotnet build "BillingExtractor.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BillingExtractor.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BillingExtractor.Api.dll"]