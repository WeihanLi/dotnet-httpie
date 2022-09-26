FROM mcr.microsoft.com/dotnet/runtime-deps:7.0-alpine AS base
LABEL Maintainer="WeihanLi"
# RUN apk add clang gcc lld musl-dev build-base zlib-dev

FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build-env

WORKDIR /app
COPY ./src/ ./src/
COPY ./build/ ./build/
COPY ./Directory.Build.props ./
COPY ./Directory.Build.targets ./
COPY ./Directory.Packages.props ./
WORKDIR /app/src/HTTPie/
RUN dotnet publish -f net7.0 -c Release --self-contained -p:AssemblyName=http -p:PublishSingleFile=true -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true -o /app/artifacts

FROM base AS final
COPY --from=build-env /app/artifacts/http /root/.dotnet/tools/http
RUN ln -s /root/.dotnet/tools/http /root/.dotnet/tools/dotnet-http
ENV PATH="/root/.dotnet/tools:${PATH}"
