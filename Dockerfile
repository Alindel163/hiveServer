# 1. Сборка приложения через официальный .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Копируем проект и восстанавливаем зависимости NuGet
COPY *.csproj ./
RUN dotnet restore

# Копируем остальной код и компилируем
COPY . ./
RUN dotnet publish -c Release -o out

# 2. Финальный образ для запуска приложения
FROM ://microsoft.com
WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 8080
ENTRYPOINT ["dotnet", "hiveServer.dll"]
