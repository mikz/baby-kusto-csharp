FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["samples/Sample.CsvServer/Sample.CsvServer.csproj", "Sample.CsvServer/"]
COPY ["src/BabyKusto.Server/BabyKusto.Server.csproj", "BabyKusto.Server/"]
COPY ["src/BabyKusto.Core/BabyKusto.Core.csproj", "BabyKusto.Core/"]

# Restore dependencies
RUN dotnet restore "Sample.CsvServer/Sample.CsvServer.csproj"

# Copy source code
COPY src/BabyKusto.Core/. BabyKusto.Core/
COPY src/BabyKusto.Server/. BabyKusto.Server/
COPY samples/Sample.CsvServer/. Sample.CsvServer/

# Build
RUN dotnet build "Sample.CsvServer/Sample.CsvServer.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "Sample.CsvServer/Sample.CsvServer.csproj" -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Create directory for CSV files
RUN mkdir /data

# Volume for CSV files
VOLUME /data

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "Sample.CsvServer.dll", "--csv", "/data/*.csv"]