#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
RUN apt-get update && apt-get install -y libgdiplus
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["WebSSH/Server/WebSSH.Server.csproj", "WebSSH/Server/"]
COPY ["WebSSH/Client/WebSSH.Client.csproj", "WebSSH/Client/"]
COPY ["WebSSH/Shared/WebSSH.Shared.csproj", "WebSSH/Shared/"]
RUN dotnet restore "WebSSH/Server/WebSSH.Server.csproj"
COPY . .
WORKDIR "/src/WebSSH/Server"
RUN dotnet build "WebSSH.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebSSH.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebSSH.Server.dll"]