FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TelegramMovieBot.sln ./
COPY src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj src/TelegramMovieBot.Api/
RUN dotnet restore src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj

COPY src/TelegramMovieBot.Api/ src/TelegramMovieBot.Api/
RUN dotnet publish src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "TelegramMovieBot.Api.dll"]
CMD ["--run-once"]
