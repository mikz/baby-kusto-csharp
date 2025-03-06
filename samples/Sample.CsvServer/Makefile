# Makefile for Sample.CsvServer

.PHONY: build test docker run clean

# Default target
all: build

# Build the application
build:
	dotnet build Sample.CsvServer.csproj

# Run tests (once test project is created)
test:
	dotnet test ../test/Sample.CsvServer.Tests/Sample.CsvServer.Tests.csproj

# Build Docker image
docker:
	docker build -f Dockerfile -t babykusto-csv-server ../../

# Run Docker container
# Usage: make run CSV_DIR=/path/to/csv/files
run:
	@if [ -z "$(CSV_DIR)" ]; then \
		echo "Error: CSV_DIR is not set. Usage: make run CSV_DIR=/path/to/csv/files"; \
		exit 1; \
	fi
	docker run -p 5000:80 -v $(CSV_DIR):/data babykusto-csv-server

# Clean build artifacts
clean:
	dotnet clean Sample.CsvServer.csproj
	rm -rf bin obj