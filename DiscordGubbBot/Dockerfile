#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
ENV LOGIN_TOKEN=
ENV GOOGLE_APPLICATION_CREDENTIALS=/app/auth/google_auth.json
RUN apt update
RUN apt install -y libc-dev

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["DiscordGubbBot/DiscordGubbBot.csproj", "DiscordGubbBot/"]
RUN dotnet restore "DiscordGubbBot/DiscordGubbBot.csproj"
COPY . .
WORKDIR "/src/DiscordGubbBot"
RUN dotnet build "DiscordGubbBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DiscordGubbBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DiscordGubbBot.dll"]