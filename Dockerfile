FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore
WORKDIR /app/TypeChatExamples
RUN dotnet publish -c release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
# install node.js, ffmpeg and npm install
RUN apt-get clean && apt-get update && apt-get upgrade -y && apt-get install -y --no-install-recommends curl gnupg ffmpeg \
    && curl -sL https://deb.nodesource.com/setup_current.x | bash - \
    && apt-get install nodejs -yq
WORKDIR /app
COPY --from=build /out ./
# don't run dev postinstall script when installing npm deps
RUN grep -Ev 'postinstall' package.json > tmp && mv tmp package.json && npm install
ENTRYPOINT ["dotnet", "TypeChatExamples.dll"]
