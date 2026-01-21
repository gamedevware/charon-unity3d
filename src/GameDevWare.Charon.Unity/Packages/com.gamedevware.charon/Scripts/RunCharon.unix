#!/bin/bash

# Add typical paths for mono and dotnet to PATH
export PATH=$PATH:/usr/local/bin:/usr/bin:/usr/sbin:/opt/homebrew/bin:/opt/local/bin:/usr/local/share/dotnet:/usr/share/dotnet:/snap/bin


# Find the current directory of the script
SCRIPT_DIR=$(cd -- "$(dirname -- "$0")" && pwd)

# Check if dotnet is installed
check_dotnet() {
    if ! command -v dotnet &> /dev/null; then
        echo ".NET SDK 8+ is not installed." >&2
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
        echo ".NET SDK version $DOTNET_VERSION is installed, while 8 or later is required." >&2
        echo "Please install .NET SDK 8 or later from https://dotnet.microsoft.com/en-us/download" >&2
        exit 1
    fi
}

# Install/Update charon tool
install_update_charon_tool() {
    pushd "$SCRIPT_DIR" > /dev/null || exit 1
    if [ ! -f ".config/dotnet-tools.json" ] && [ ! -f "dotnet-tools.json" ]; then
        dotnet new tool-manifest -o . > /dev/null 2>&1;
    fi

    # Fix .NET SDK 10 behaviour: Move manifest to .config if it was created in root
    if [ -f "dotnet-tools.json" ]; then
        # Ensure the .config directory exists (-p prevents error if it already exists)
        mkdir -p .config
        
        # Move the file to the expected location
        mv -f "dotnet-tools.json" ".config/dotnet-tools.json" > /dev/null 2>&1
    fi

    if dotnet tool list --local | grep -q 'dotnet-charon'; then
        dotnet tool update dotnet-charon --local --tool-manifest .config/dotnet-tools.json > /dev/null 2>&1;
    else
        dotnet tool install dotnet-charon --local --tool-manifest .config/dotnet-tools.json > /dev/null 2>&1;

        if [ $? -ne 0 ] && [ $? -ne 1 ]; then
            echo "Failed to execute the 'dotnet tool install dotnet-charon' command to retrieve the latest package version from NuGet." >&2
            echo "Ensure that the 'dotnet' tool is installed and available in the 'PATH'." >&2
            echo "Check https://dotnet.microsoft.com/en-us/download for the installer." >&2
            popd > /dev/null || exit 1
            exit 1
        fi
    fi
    popd > /dev/null || exit 1
}

# Run charon tool with specified parameters
run_tool() {
    pushd "$SCRIPT_DIR" > /dev/null || exit 1
    dotnet charon "$@"
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
install_update_charon_tool
run_tool "$@"

# Exit successfully
exit 0
