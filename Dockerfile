FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build-env
ARG TARGETARCH

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
RUN dotnet publish -f net9.0 --use-current-runtime -a $TARGETARCH -p:AssemblyName=http -p:TargetFrameworks=net9.0 -o /app/artifacts

FROM alpine

# https://github.com/opencontainers/image-spec/blob/main/annotations.md
LABEL org.opencontainers.image.authors="WeihanLi"
LABEL org.opencontainers.image.source="https://github.com/WeihanLi/dotnet-httpie"

COPY --from=build-env /app/artifacts/http /app/http
ENV PATH="/app:${PATH}"
ENTRYPOINT ["/app/http"]
CMD ["--help"]
