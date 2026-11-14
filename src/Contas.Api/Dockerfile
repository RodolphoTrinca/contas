FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Contas.Domain/ src/Contas.Domain/
COPY src/Contas.Application/ src/Contas.Application/
COPY src/Contas.Infrastructure/ src/Contas.Infrastructure/
COPY src/Contas.Api/ src/Contas.Api/
RUN dotnet restore src/Contas.Api --locked-mode
RUN dotnet publish src/Contas.Api --no-restore -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /publish .
USER app
ENTRYPOINT ["dotnet", "Contas.Api.dll"]
