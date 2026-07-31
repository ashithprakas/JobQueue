FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["JobQueue.RetrySweepWorker/JobQueue.RetrySweepWorker.csproj","JobQueue.RetrySweepWorker/"]
COPY ["JobQueue.Core/JobQueue.Core.csproj","JobQueue.Core/"]
COPY ["JobQueue.Infrastructure/JobQueue.Infrastructure.csproj","JobQueue.Infrastructure/"]

RUN dotnet restore "JobQueue.RetrySweepWorker/JobQueue.RetrySweepWorker.csproj"

COPY . .
WORKDIR /src/JobQueue.RetrySweepWorker
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT [ "dotnet","JobQueue.RetrySweepWorker.dll" ]
