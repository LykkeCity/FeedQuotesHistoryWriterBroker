FROM microsoft/dotnet:2.0-runtime
ARG SOURCEDIR
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "FeedQuotesHistoryWriterBroker.dll"]
