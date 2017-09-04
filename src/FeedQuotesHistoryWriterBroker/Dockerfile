FROM microsoft/dotnet:2.0-sdk
ARG SOURCEDIR
WORKDIR /brocker
COPY ${SOURCEDIR} .
ENTRYPOINT ["dotnet", "QuotesWriter.Broker.dll"]
