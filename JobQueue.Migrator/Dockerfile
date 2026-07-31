FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

COPY ["JobQueue.Infrastructure/JobQueue.Infrastructure.csproj","JobQueue.Infrastructure/"]
COPY ["JobQueue.Core/JobQueue.Core.csproj","JobQueue.Core/"]

RUN dotnet restore "JobQueue.Infrastructure/JobQueue.Infrastructure.csproj"

COPY . .
WORKDIR /src/JobQueue.Infrastructure
RUN dotnet ef migrations bundle -o efbundle --self-contained -r linux-arm64

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final
WORKDIR /app
COPY --from=build /src/JobQueue.Infrastructure/efbundle .

ENTRYPOINT ["sh", "-c", "./efbundle --connection \"$ConnectionStrings__DefaultConnection\""]
