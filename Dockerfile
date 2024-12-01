FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
ARG TARGETARCH

# Configure NativeAOT Build Prerequisites 
# https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=linux-alpine%2Cnet8
# for alpine
# RUN apk update && apk add clang build-base zlib-dev
# for debian/ubuntu
# https://github.com/dotnet/runtimelab/issues/1785#issuecomment-993179119
RUN apt-get update && apt-get install -y clang zlib1g-dev binutils-aarch64-linux-gnu

WORKDIR /app

COPY ./src/ ./src/
COPY ./build/ ./build/
COPY ./Directory.Build.props ./
COPY ./Directory.Build.targets ./
COPY ./Directory.Packages.props ./
COPY ./.editorconfig ./

WORKDIR /app/src/HTTPie/
RUN dotnet publish -f net9.0 --use-current-runtime -a $TARGETARCH -p:AssemblyName=http -p:TargetFrameworks=net9.0 -o /app/artifacts

FROM scratch

# https://github.com/opencontainers/image-spec/blob/main/annotations.md
LABEL org.opencontainers.image.authors="WeihanLi"
LABEL org.opencontainers.image.source="https://github.com/WeihanLi/dotnet-httpie"

WORKDIR /app
COPY --from=build-env /app/artifacts/http /app/http
ENV PATH="/app:${PATH}"
ENTRYPOINT ["/app/http"]
CMD ["--help"]
