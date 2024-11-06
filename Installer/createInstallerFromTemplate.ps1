# Description: This script is used to create an installer from a template file (installer.iss) by replacing a placeholder string with the SHA256 hash of the Browser.exe file.
param (
    [Parameter()]
    [string]
    $sysarch = "win-x64"
)

# Get the directory of the current script
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
#write-host "Script Directory: $scriptDir"

# Set the path to the Browser.exe file (relative to the script location)
$browserPath = Join-Path -Path "$scriptDir\..\Browser\Build\Browser\net472\$sysarch" -ChildPath "Browser.exe"
#write-host "Browser Path: $browserPath"

# Set the path to the original file to read from (relative to the script location)
$originalFilePath = Join-Path -Path $scriptDir -ChildPath "installer.iss"
#write-host "Original File Path: $originalFilePath"

# Set the path to the new file (Installer.iss) where the updated content will be saved
$outputFilePath = Join-Path -Path "$scriptDir\..\Browser\Build\Installer" -ChildPath "installer.iss"
#write-host "Output File Path: $outputFilePath"

# Function to calculate SHA256 hash
function Get-SHA256Hash {
    param (
        [string]$filePath
    )

    # Create a SHA256 hash object
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    
    # Read the file and compute the hash
    $fileStream = [System.IO.File]::OpenRead($filePath)
    $hashBytes = $sha256.ComputeHash($fileStream)
    $fileStream.Close()

    # Convert hash bytes to hexadecimal string
    return [BitConverter]::ToString($hashBytes) -replace '-', ''
}

# Get the SHA256 hash of Browser.exe
$hash = Get-SHA256Hash -filePath $browserPath

# Read the original file and replace the specified string with the hash
$content = Get-Content $originalFilePath
$updatedContent = $content -replace [regex]::Escape("{{BROWSEREXEHASH}}"), $hash

# Add sysarch to the updated content
$updatedContent = $updatedContent -replace [regex]::Escape("{{SYSARCH}}"), $sysarch

# Output the new hash
Write-Host "The SHA256 hash of Browser.exe is: $hash"

# Define destination path
$destinationPath = "$scriptDir\..\Browser\Build\Installer"

# Remove the existing Installer directory
Remove-Item -Path $destinationPath -Recurse -Force -ErrorAction SilentlyContinue

# Copy the Installer files to the destination path
Copy-Item -Path "$scriptDir" -Destination $destinationPath -Recurse -Force

# Save the updated content to the new file Installer.iss
$updatedContent | Set-Content $outputFilePath