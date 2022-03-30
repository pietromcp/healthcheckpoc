FROM mcr.microsoft.com/dotnet/sdk:6.0 AS builder
ARG APP_VERSION=unknown
WORKDIR /sources
COPY HealthChecksPoc.sln .
COPY **/*.csproj ./
RUN for file in *.csproj ; do prj=$(echo $file | sed 's/[.]csproj//' ) ; mkdir $prj; mv $file $prj/$file ; done
RUN dotnet restore
COPY . ./
RUN dotnet publish HealthChecksPoc/HealthChecksPoc.csproj -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
COPY --from=builder /app /app
WORKDIR /app
ENTRYPOINT ["./HealthChecksPoc"]
