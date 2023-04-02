FROM mcr.microsoft.com/dotnet/runtime-deps:7.0-alpine AS base
LABEL Maintainer="WeihanLi"

FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build-env

# Install NativeAOT build prerequisites 
# RUN apk update && apk add clang gcc lld musl-dev build-base zlib-dev

WORKDIR /app
COPY ./src/ ./src/
COPY ./build/ ./build/
COPY ./Directory.Build.props ./
COPY ./Directory.Build.targets ./
COPY ./Directory.Packages.props ./
WORKDIR /app/src/HTTPie/
RUN dotnet publish -f net7.0 -c Release --self-contained --use-current-runtime -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:AssemblyName=http -p:TargetFrameworks=net7.0 -o /app/artifacts

FROM base AS final
COPY --from=build-env /app/artifacts/http /root/.dotnet/tools/http
RUN ln -s /root/.dotnet/tools/http /root/.dotnet/tools/dotnet-http
ENV PATH="/root/.dotnet/tools:${PATH}"
