FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS publish
WORKDIR /src

COPY ["ReisAPI.csproj", "."]
RUN dotnet restore "ReisAPI.csproj"
COPY [".", "."]
RUN dotnet build "ReisAPI.csproj" -c Release -o /app/build

RUN dotnet publish "ReisAPI.csproj" -c Release -o /app/publish \
  --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 80
ENV ASPNETCORE_URLS=http://*:80
ENTRYPOINT ["dotnet", "./ReisAPI.dll"]