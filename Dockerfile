FROM microsoft/dotnet:2.0-runtime
WORKDIR /app
ENV S3Sync_LocalRoot=/app/sync
COPY source/S3Sync/obj/Docker/publish .
CMD ["dotnet", "S3Sync.dll"]
