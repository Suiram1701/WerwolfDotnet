FROM node:20-alpine AS frontend-build
WORKDIR /app/frontend

COPY ./WerwolfDotnet.Client/package.json ./WerwolfDotnet.Client/package-lock.json ./
RUN npm ci

COPY ./WerwolfDotnet.Client/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /app

COPY ./WerwolfDotnet.Server/*.csproj ./Backend/
RUN dotnet restore ./Backend/WerwolfDotnet.Server.csproj 

COPY ./WerwolfDotnet.Server/ ./Backend/
COPY ./WerwolfDotnet/ ./WerwolfDotnet/
COPY --from=frontend-build /app/frontend/build ./Backend/wwwroot/

RUN dotnet publish ./Backend/WerwolfDotnet.Server.csproj \
    -c Release \
    -o /app/publish 
#    --no-restore    # dotnet restore can't restore everything by itself (but publish can however)

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=backend-build /app/publish ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "WerwolfDotnet.Server.dll"]
