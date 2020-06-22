FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY YtToVkReposter/*.csproj ./YtToVkReposter/
RUN dotnet restore -r linux-musl-x64

# copy everything else and build app
COPY YtToVkReposter/. ./YtToVkReposter/
WORKDIR /app/YtToVkReposter
RUN dotnet publish -c Release -o /app -r linux-musl-x64 --self-contained false --no-restore

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/YtToVkReposter ./
ENTRYPOINT ["./YtToVkReposter"]
