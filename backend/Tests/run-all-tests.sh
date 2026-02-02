#!/bin/bash
# Simple test runner - runs all functional tests
# Usage: ./run-all-tests.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== Running All Valora Functional Tests ==="
echo ""

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: dotnet CLI is not installed or not in PATH"
    echo "Please install .NET SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

cd "$SCRIPT_DIR"

echo "Running all tests in Valora.Tests.csproj..."
echo ""

# Run all tests with detailed output
dotnet test Valora.Tests.csproj \
    --verbosity normal \
    --logger "console;verbosity=detailed" \
    --collect:"XPlat Code Coverage"

EXIT_CODE=$?

echo ""
if [ $EXIT_CODE -eq 0 ]; then
    echo "✅ All tests passed successfully!"
else
    echo "❌ Some tests failed. Review the output above for details."
fi

exit $EXIT_CODE
