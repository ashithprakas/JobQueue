FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["JobQueue.API/JobQueue.API.csproj","JobQueue.API/"]
COPY ["JobQueue.Application/JobQueue.Application.csproj","JobQueue.Application/"]
COPY ["JobQueue.Core/JobQueue.Core.csproj","JobQueue.Core/"]
COPY ["JobQueue.Infrastructure/JobQueue.Infrastructure.csproj","JobQueue.Infrastructure/"]
RUN dotnet restore "JobQueue.API/JobQueue.API.csproj"

COPY . .
WORKDIR /src/JobQueue.API
RUN dotnet publish -c Release -o /app/publish /p:useAppHost=false 

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT [ "dotnet" ,"JobQueue.API.dll"]

