FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine AS base
LABEL Maintainer="WeihanLi"
# configure aot Prerequisites https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=linux-alpine%2Cnet8
RUN apk add clang build-base zlib-dev

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env

# Install NativeAOT build prerequisites 
# RUN apk update && apk add clang gcc lld musl-dev build-base zlib-dev

WORKDIR /app
COPY ./src/ ./src/
COPY ./build/ ./build/
COPY ./Directory.Build.props ./
COPY ./Directory.Build.targets ./
COPY ./Directory.Packages.props ./
WORKDIR /app/src/HTTPie/
RUN dotnet publish -f net8.0 --use-current-runtime -p:AssemblyName=http -o /app/artifacts

FROM base AS final
COPY --from=build-env /app/artifacts/http /root/.dotnet/tools/http
RUN ln -s /root/.dotnet/tools/http /root/.dotnet/tools/dotnet-http
ENV PATH="/root/.dotnet/tools:${PATH}"
WORKDIR /root/.dotnet/tools/
ENTRYPOINT ["/root/.dotnet/tools/http"]
CMD ["--help"]
