FROM microsoft/dotnet:2.0-runtime
ARG SOURCEDIR
WORKDIR /brocker
COPY ${SOURCEDIR} .
ENTRYPOINT ["dotnet", "QuotesWriter.Broker.dll"]
