FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["StreamForge.sln", "./"]
COPY ["src/StreamForge.Domain/StreamForge.Domain.csproj", "src/StreamForge.Domain/"]
COPY ["src/StreamForge.Application/StreamForge.Application.csproj", "src/StreamForge.Application/"]
COPY ["src/StreamForge.Infrastructure/StreamForge.Infrastructure.csproj", "src/StreamForge.Infrastructure/"]
COPY ["src/StreamForge.Api/StreamForge.Api.csproj", "src/StreamForge.Api/"]

RUN dotnet restore "src/StreamForge.Api/StreamForge.Api.csproj"

COPY . .
RUN dotnet publish "src/StreamForge.Api/StreamForge.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
RUN mkdir -p /app/data/uploads /app/data/transcription-output /app/data/keys

ENV ASPNETCORE_URLS=http://+:8080
ENV Storage__Local__RootPath=/app/data
ENV VideoProcessing__FfmpegPath=/usr/bin/ffmpeg
ENV VideoProcessing__FfprobePath=/usr/bin/ffprobe

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "StreamForge.Api.dll"]
