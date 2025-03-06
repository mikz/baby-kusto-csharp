FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy source code
COPY . .

RUN dotnet build samples/Sample.CsvServer

# Build
RUN dotnet build samples/Sample.CsvServer -c Release -o /app/build

# Publish
RUN dotnet publish samples/Sample.CsvServer -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Create directory for CSV files
RUN mkdir /data

# Volume for CSV files
VOLUME /data

EXPOSE 5220
ENV ASPNETCORE_ENVIRONMENT=production
ENTRYPOINT ["dotnet", "Sample.CsvServer.dll"]