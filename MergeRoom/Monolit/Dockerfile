FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS publish


ARG SRCDIR="src"
ARG APPNAME=""
ARG APPATH=""
ARG NUGETSRC="https://api.nuget.org/v3/index.json"

COPY ${SRCDIR}/ /src/
COPY shared/ /shared/

WORKDIR /src
RUN dotnet restore "${APPATH}${APPNAME}/${APPNAME}.csproj" 
RUN dotnet publish "${APPATH}${APPNAME}/${APPNAME}.csproj" --no-restore -c Release -o /app/publish -p:PublishTrimmed=false
RUN echo "#!/bin/bash\nset -e\nruncmd=\"dotnet ${APPNAME}.dll\"\nexec \$runcmd" > /app/publish/entrypoint.sh && chmod +x /app/publish/entrypoint.sh

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
ENV TZ="UTC"
ENV URLS="http://+:80"
EXPOSE 80/tcp
ENTRYPOINT ["./entrypoint.sh"]
