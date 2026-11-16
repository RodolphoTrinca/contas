FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY Directory.Build.props Directory.Packages.props ./
COPY samples/Contas.PartnerDemo/ samples/Contas.PartnerDemo/
RUN dotnet restore samples/Contas.PartnerDemo --locked-mode
RUN dotnet publish samples/Contas.PartnerDemo --no-restore -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /publish .
RUN mkdir /data && chown app:app /data
USER app
ENTRYPOINT ["dotnet", "Contas.PartnerDemo.dll"]
