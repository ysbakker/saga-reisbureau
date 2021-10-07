FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS publish
WORKDIR /src

COPY ["HuurautoService.csproj", "."]
RUN dotnet restore "HuurautoService.csproj"
COPY [".", "."]
RUN dotnet build "HuurautoService.csproj" -c Release -o /app/build

RUN dotnet publish "HuurautoService.csproj" -c Release -o /app/publish \
  --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 80
ENV ASPNETCORE_URLS=http://*:80
ENTRYPOINT ["dotnet", "./HuurautoService.dll"]