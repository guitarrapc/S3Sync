FROM microsoft/dotnet:2.0-runtime
ARG source
WORKDIR /app
ENV S3Sync_LocalRoot=/app/sync
COPY ${source:-obj/Docker/publish} .
CMD ["dotnet", "S3Sync.dll"]
