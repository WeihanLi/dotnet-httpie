FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-arm64-musl AS cross-build-env

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine-aot AS build-env

COPY --from=cross-build-env /crossrootfs /crossrootfs

ARG TARGETARCH
ARG BUILDARCH

# Configure NativeAOT Build Prerequisites 
# https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=linux-alpine%2Cnet8
# for alpine
RUN apk update && apk add clang build-base zlib-dev
# for debian/ubuntu
# RUN apt-get update && apt-get install -y clang zlib1g-dev

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
      apk add binutils-aarch64 --repository=https://dl-cdn.alpinelinux.org/alpine/edge/community; \
      dotnet publish -f net9.0 -r linux-musl-arm64 -p:AssemblyName=http -p:TargetFrameworks=net9.0 -p:SysRoot=/crossrootfs/arm64 -p:ObjCopyName=aarch64-alpine-linux-musl-objcopy -o /app/artifacts; \
    fi

FROM alpine

# https://github.com/opencontainers/image-spec/blob/main/annotations.md
LABEL org.opencontainers.image.authors="WeihanLi"
LABEL org.opencontainers.image.source="https://github.com/WeihanLi/dotnet-httpie"

COPY --from=build-env /app/artifacts/http /usr/bin/http
RUN chmod +x /usr/bin/http
ENTRYPOINT ["/usr/bin/http"]
CMD ["--help"]
