#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update; apt-get install -y fontconfig fonts-liberation
RUN fc-cache -f -v

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["WebSSH/Server/WebSSH.Server.csproj", "WebSSH/Server/"]
COPY ["WebSSH/Client/WebSSH.Client.csproj", "WebSSH/Client/"]
COPY ["WebSSH/Shared/WebSSH.Shared.csproj", "WebSSH/Shared/"]
RUN dotnet restore "WebSSH/Server/WebSSH.Server.csproj"
COPY . .
WORKDIR "/src/WebSSH/Server"
RUN dotnet build "WebSSH.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebSSH.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebSSH.Server.dll"]