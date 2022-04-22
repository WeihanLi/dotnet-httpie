FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine AS base
LABEL Maintainer="WeihanLi"

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build-env

WORKDIR /app
COPY ./src/ ./src/
COPY ./build/ ./build/
COPY ./Directory.Build.props ./
COPY ./Directory.Build.targets ./
COPY ./Directory.Packages.props ./
WORKDIR /app/src/HTTPie/
RUN dotnet publish -c Release --self-contained --use-current-runtime -p:AssemblyName=http -p:PublishSingleFile=true -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true -o /app/artifacts

FROM base AS final
COPY --from=build-env /app/artifacts/http /root/.dotnet/tools/http
ENV PATH="/root/.dotnet/tools:${PATH}"