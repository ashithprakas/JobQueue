FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["JobQueue.Worker/JobQueue.Worker.csproj","JobQueue.Worker/"]
COPY ["JobQueue.Application/JobQueue.Application.csproj","JobQueue.Application/"]
COPY ["JobQueue.Core/JobQueue.Core.csproj","JobQueue.Core/"]
COPY ["JobQueue.Infrastructure/JobQueue.Infrastructure.csproj","JobQueue.Infrastructure/"]

RUN dotnet restore "JobQueue.Worker/JobQueue.Worker.csproj"

COPY . .
WORKDIR /src/JobQueue.Worker
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app 
COPY --from=build /app/publish .

ENTRYPOINT [ "dotnet","JobQueue.Worker.dll" ]
