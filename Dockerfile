# 1. Используем официальный образ .NET SDK напрямую из реестра Docker Hub
FROM dotnet/sdk:8.0 AS build-env
WORKDIR /app

# 2. Копируем файлы проекта и восстанавливаем зависимости NuGet
COPY *.csproj ./
RUN dotnet restore

# 3. Копируем оставшийся код и собираем сервер в режиме Release
COPY . ./
RUN dotnet publish -c Release -o out

# 4. Создаем финальный легкий образ для запуска приложения
FROM dotnet/runtime:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# 5. Открываем порт 8080 наружу
EXPOSE 8080

# 6. Команда для запуска сервера
ENTRYPOINT ["dotnet", "hiveServer.dll"]
