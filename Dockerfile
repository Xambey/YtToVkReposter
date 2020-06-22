FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY YtToVkReposter/*.csproj ./YtToVkReposter/
RUN dotnet restore -r linux-musl-x64

# copy everything else and build app
COPY YtToVkReposter/. ./YtToVkReposter/
WORKDIR /source/YtToVkReposter
RUN dotnet publish -c Release -o /app -r linux-musl-x64 --self-contained false --no-restore /p:PublishTrimmed=true

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine AS runtime
WORKDIR /app
COPY --from=build /app/ ./
ENTRYPOINT ["./YtToVkReposter"]
