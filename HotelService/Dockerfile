FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS publish
WORKDIR /src

COPY ["HotelService.csproj", "."]
RUN dotnet restore "HotelService.csproj"
COPY [".", "."]
RUN dotnet build "HotelService.csproj" -c Release -o /app/build

RUN dotnet publish "HotelService.csproj" -c Release -o /app/publish \
  --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 80
ENV ASPNETCORE_URLS=http://*:80
ENTRYPOINT ["dotnet", "./HotelService.dll"]