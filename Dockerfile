FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine AS base
LABEL Maintainer="WeihanLi"

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build-env
# dotnet-httpie version, docker build --build-arg TOOL_VERSION=0.1.0 -t weihanli/dotnet-httpie:0.1.0 .
ARG TOOL_VERSION
RUN dotnet tool install --global dotnet-httpie --version ${TOOL_VERSION}

FROM base AS final
COPY --from=build-env /root/.dotnet/tools /root/.dotnet/tools
ENV PATH="/root/.dotnet/tools:${PATH}"