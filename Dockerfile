# 1. Сборка приложения
FROM ://microsoft.com AS build-env
WORKDIR /app

# Копируем проект и восстанавливаем зависимости NuGet
COPY *.csproj ./
RUN dotnet restore

# Копируем остальной код и компилируем
COPY . ./
RUN dotnet publish -c Release -o out

# 2. Запуск приложения (финальный легкий контейнер)
FROM ://microsoft.com
WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 8080
ENTRYPOINT ["dotnet", "hiveServer.dll"]