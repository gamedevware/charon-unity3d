#!/bin/bash

# Add typical paths for mono and dotnet to PATH
export PATH=$PATH:/usr/local/bin:/usr/bin:/usr/sbin:/opt/homebrew/bin:/opt/local/bin:/usr/local/share/dotnet:/usr/share/dotnet:/snap/bin


# Find the current directory of the script
SCRIPT_DIR=$(cd "`dirname "$0"`" && pwd)

# Check if dotnet is installed
check_dotnet() {
    if ! command -v dotnet &> /dev/null; then
        echo ".NET SDK is not installed." >&2
        echo "Please install .NET SDK from https://dotnet.microsoft.com/en-us/download" >&2
        exit 1
    fi
}

# Get the installed dotnet version
get_dotnet_version() {
    DOTNET_VERSION=$(dotnet --version 2>/dev/null)
    if [ -z "$DOTNET_VERSION" ]; then
        echo "Failed to retrieve .NET version. Ensure 'dotnet' is installed and available in the PATH." >&2
        exit 1
    fi
    MAJOR_VERSION=$(echo "$DOTNET_VERSION" | cut -d. -f1)
}

# Check if the major version is 8 or later
check_dotnet_version() {
    if [ "$MAJOR_VERSION" -lt 8 ]; then
        echo ".NET version $DOTNET_VERSION is installed, but it is not version 8 or later." >&2
        echo "Please install .NET 8 or later from https://dotnet.microsoft.com/en-us/download" >&2
        exit 1
    fi
}

# Install/Update t4 tool
install_update_t4_tool() {
    pushd "$SCRIPT_DIR" > /dev/null || exit 1
    if [ ! -f ".config/dotnet-tools.json" ]; then
        dotnet new tool-manifest -o . > /dev/null 2>&1;
    fi
    if dotnet tool list --local | grep -q 'dotnet-t4'; then
        dotnet tool update dotnet-t4 --local --tool-manifest .config/dotnet-tools.json > /dev/null 2>&1;
    else
        if ! dotnet tool install dotnet-t4 --local --tool-manifest .config/dotnet-tools.json > /dev/null 2>&1; then
            echo "Failed to execute the 'dotnet tool install dotnet-t4' command to retrieve the latest package version from NuGet." >&2
            echo "Ensure that the 'dotnet' tool is installed and available in the 'PATH'." >&2
            echo "Check https://dotnet.microsoft.com/en-us/download for the installer." >&2
            popd > /dev/null || exit 1
            exit 1
        fi
    fi
    popd > /dev/null || exit 1
}

# Run t4 tool with specified parameters
run_tool() {
    pushd "$SCRIPT_DIR" > /dev/null || exit 1
    dotnet t4 "$@"
    EXITCODE=$?
    popd > /dev/null || exit 1

    if [ "$EXITCODE" -ne 0 ]; then
        exit "$EXITCODE"
    fi
}

# Main script execution
check_dotnet
get_dotnet_version
check_dotnet_version
install_update_t4_tool
run_tool "$@"

# Exit successfully
exit 0
