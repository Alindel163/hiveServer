# 1. Берем официальный SDK от Microsoft для сборки проекта
FROM ://microsoft.com AS build-env
WORKDIR /app

# 2. Копируем файлы проекта и восстанавливаем зависимости NuGet
COPY *.csproj ./
RUN dotnet restore

# 3. Копируем оставшийся код и собираем сервер в режиме Release
COPY . ./
RUN dotnet publish -c Release -o out

# 4. Создаем финальный легкий образ для запуска
FROM ://microsoft.com
WORKDIR /app
COPY --from=build-env /app/out .

# 5. Открываем порт 8080 наружу для сети Fleck
EXPOSE 8080

# 6. Команда для запуска сервера
ENTRYPOINT ["dotnet", "HiveServer.dll"]
