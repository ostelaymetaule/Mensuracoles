#echo "build & run"
#docker build -t dndb-cards-bot . 
#docker run -d --env-file ./env -p 8091:80  -t dndb-cards-bot:latest
#echo "done!"


# FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
# COPY *.sln ./
COPY Mensuracoles/*.csproj ./Mensuracoles/
RUN dotnet restore Mensuracoles/*.csproj

COPY Mensuracoles/ Mensuracoles/
 

WORKDIR /source/Mensuracoles
RUN dotnet build -c release --no-restore

FROM build AS publish
RUN dotnet publish -c release --no-build -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1

WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Mensuracoles.dll"]
