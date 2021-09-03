FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine AS base
LABEL Maintainer="WeihanLi"

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build-env

WORKDIR /app
RUN dotnet publish ./src/HTTPie/HTTPie.csproj -c Release --self-contained --use-current-runtime -p:AssemblyName=http -o ./artifacts

FROM base AS final
COPY --from=build-env /app/artifacts /root/.dotnet/tools
ENV PATH="/root/.dotnet/tools:${PATH}"