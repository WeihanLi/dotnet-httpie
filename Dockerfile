FROM mcr.microsoft.com/dotnet/runtime:3.1-alpine AS base
LABEL Maintainer="WeihanLi"

FROM mcr.microsoft.com/dotnet/sdk:3.1-alpine AS build-env
# dotnet-httpie version
ARG TOOL_VERSION
RUN dotnet tool install --global dotnet-httpie --version ${TOOL_VERSION}

FROM base AS final
COPY --from=build-env /root/.dotnet/tools /root/.dotnet/tools
ENV PATH="/root/.dotnet/tools:${PATH}"