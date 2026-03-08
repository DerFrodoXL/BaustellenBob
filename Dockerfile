FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY BaustellenBob.slnx .
COPY src/BaustellenBob.Domain/BaustellenBob.Domain.csproj src/BaustellenBob.Domain/
COPY src/BaustellenBob.Shared/BaustellenBob.Shared.csproj src/BaustellenBob.Shared/
COPY src/BaustellenBob.Application/BaustellenBob.Application.csproj src/BaustellenBob.Application/
COPY src/BaustellenBob.Infrastructure/BaustellenBob.Infrastructure.csproj src/BaustellenBob.Infrastructure/
COPY src/BaustellenBob.Server/BaustellenBob.Server.csproj src/BaustellenBob.Server/
RUN dotnet restore BaustellenBob.slnx

# Copy everything and publish
COPY . .
RUN dotnet publish src/BaustellenBob.Server/BaustellenBob.Server.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Create uploads directory
RUN mkdir -p /app/uploads

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BaustellenBob.Server.dll"]
