FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AnxietyWatch.Web.sln global.json ./
COPY AnxietyWatch.Web/AnxietyWatch.Web.csproj AnxietyWatch.Web/
COPY AnxietyWatch.Web.Client/AnxietyWatch.Web.Client.csproj AnxietyWatch.Web.Client/
COPY AnxietyWatch.Web.Client.Tests/AnxietyWatch.Web.Client.Tests.csproj AnxietyWatch.Web.Client.Tests/
RUN dotnet restore AnxietyWatch.Web.sln

COPY . .
RUN dotnet publish AnxietyWatch.Web/AnxietyWatch.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish
RUN test -f /app/publish/wwwroot/_framework/blazor.web.js \
    && grep -q '"Route":"_framework/blazor.web.js"' \
        /app/publish/AnxietyWatch.Web.staticwebassets.endpoints.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "AnxietyWatch.Web.dll"]
