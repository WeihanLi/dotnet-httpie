FROM --platform=$BUILDPLATFORM  mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env

# Configure NativeAOT Build Prerequisites 
# https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=linux-alpine%2Cnet8
RUN apk add clang build-base zlib-dev

WORKDIR /app

COPY ./src/ ./src/
COPY ./build/ ./build/
COPY ./Directory.Build.props ./
COPY ./Directory.Build.targets ./
COPY ./Directory.Packages.props ./
COPY ./.editorconfig ./

WORKDIR /app/src/HTTPie/
RUN dotnet publish -f net8.0 --use-current-runtime -a $TARGETARCH -p:AssemblyName=http -o /app/artifacts

FROM scratch
LABEL Maintainer="WeihanLi"
WORKDIR /app
COPY --from=build-env /app/artifacts/http /app/http
ENV PATH="/app:${PATH}"
ENTRYPOINT ["/app/http"]
CMD ["--help"]
