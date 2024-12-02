
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net9.0-cross-arm64 AS cross-build-env

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env

COPY --from=cross-build-env /crossrootfs /crossrootfs

ARG TARGETARCH
ARG BUILDARCH

# Configure NativeAOT Build Prerequisites 
# https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=linux-alpine%2Cnet8
# for alpine
# RUN apk update && apk add clang build-base zlib-dev
# for debian/ubuntu
RUN apt-get update && apt-get install -y clang zlib1g-dev binutils-aarch64-linux-gnu

WORKDIR /app

COPY ./src/ ./src/
COPY ./build/ ./build/
COPY ./Directory.Build.props ./
COPY ./Directory.Build.targets ./
COPY ./Directory.Packages.props ./
COPY ./.editorconfig ./

WORKDIR /app/src/HTTPie/

RUN if [ "${TARGETARCH}" = "${BUILDARCH}" ]; then \
      dotnet publish -f net9.0 --use-current-runtime -p:AssemblyName=http -p:TargetFrameworks=net9.0 -o /app/artifacts; \
    else \
      dotnet publish -f net9.0 -r linux-arm64 -p:AssemblyName=http -p:TargetFrameworks=net9.0 -p:SysRoot=/crossrootfs/arm64 -p:ObjCopyName=aarch64-linux-gnu-objcopy -o /app/artifacts; \
    fi

RUN apt install -y file && file /app/artifacts/http

FROM scratch

# https://github.com/opencontainers/image-spec/blob/main/annotations.md
LABEL org.opencontainers.image.authors="WeihanLi"
LABEL org.opencontainers.image.source="https://github.com/WeihanLi/dotnet-httpie"

COPY --from=build-env /app/artifacts/http /app/http
ENV PATH="/app:${PATH}"
ENTRYPOINT ["/app/http"]
CMD ["--help"]
