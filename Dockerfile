FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-net10.0-cross-arm64-musl AS cross-build-env

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine-aot AS build-env

COPY --from=cross-build-env /crossrootfs /crossrootfs

ARG TARGETARCH
ARG BUILDARCH

WORKDIR /app

COPY ./src/ ./src/
COPY ./build/ ./build/
COPY ./Directory.Build.props ./
COPY ./Directory.Build.targets ./
COPY ./Directory.Packages.props ./
COPY ./.editorconfig ./

WORKDIR /app/src/HTTPie/

RUN curl -fsSL https://github.com/WeihanLi/dotnet-install/releases/download/v0.2.0-preview-2/dotnet-install-0.2.0-preview-2-linux-musl-x64 -o ./artifacts/install && chmod +x ./artifacts/install && ./artifacts/install version

RUN if [ "${TARGETARCH}" = "${BUILDARCH}" ]; then \
      dotnet publish -f net10.0 --use-current-runtime -p:AssemblyName=http -p:TargetFrameworks=net10.0 -o /app/artifacts; \
    else \
      apk add binutils-aarch64 --repository=https://dl-cdn.alpinelinux.org/alpine/edge/community; \
      dotnet publish -f net10.0 -r linux-musl-arm64 -p:AssemblyName=http -p:TargetFrameworks=net10.0 -p:SysRoot=/crossrootfs/arm64 -p:ObjCopyName=aarch64-alpine-linux-musl-objcopy -o /app/artifacts; \
      curl -fsSL https://github.com/WeihanLi/dotnet-install/releases/download/v0.2.0-preview-2/dotnet-install-0.2.0-preview-2-linux-musl-arm64 -o ./artifacts/install && chmod +x ./artifacts/install && ./artifacts/install version; \
    fi

FROM alpine

# https://github.com/opencontainers/image-spec/blob/main/annotations.md
LABEL org.opencontainers.image.authors="WeihanLi"
LABEL org.opencontainers.image.source="https://github.com/WeihanLi/dotnet-httpie"

COPY --from=build-env /app/artifacts/http /usr/bin/http
RUN chmod +x /usr/bin/http
ENTRYPOINT ["/usr/bin/http"]
CMD ["--help"]
