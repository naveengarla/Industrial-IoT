param(
    [string]
    $IAIStorageAccountName,

    [string]
    $IAIStorageAccountContainerName,

    [string]
    $IAIVersion,

    [string]
    $IAILocalFolder
)

if (!$IAIStorageAccountName) {
    Write-Host "##vso[task.complete result=Failed]IAIStorageAccountName not set, exiting."
}

if (!$IAIStorageAccountContainerName) {
    Write-Host "##vso[task.complete result=Failed]IAIStorageAccountContainerName not set, exiting."
}

if (!$IAILocalFolder) {
    $IAILocalFolder = $PSScriptRoot
    Write-Host "##vso[task.logissue type=warning]IAILocalFolder not set, using $($IAILocalFolder)."
}

if (!$IAIVersion) {
    Write-Host "IAIVersion not set, using latest..."
    $IAIVersion = "latest"
}

Import-Module Azure.Storage -Force

$context = New-AzureStorageContext -StorageAccountName azureiiot -Anonymous

if (!$context) {
    Write-Host "##vso[task.complete result=Failed]Could not retrieve storage context with name '$($IAIStorageAccountName), exiting.'"
}

$blobObjects = Get-AzureStorageBlob -Container $IAIStorageAccountContainerName -Context $context

if (!$blobObjects) {
    Write-Host "##vso[task.complete result=Failed]Could not get blob contents in storage account '$($IAIStorageAccountName), container '$($IAIStorageAccountContainerName)', exiting.'"
}

$blobObjects = $blobObjects | ?{ $_.Name.StartsWith("master") -and $_.Name.EndsWith(".exe") }

if ($IAIVersion -eq "latest") {
    $blobObject = $blobObjects | sort -Descending Name | select -First 1
} else {
    $blobObject = $blobObject | ?{ $_.name -contains $IAIVersion } | select -First 1
}

$version = $blobObject.Name.Split("/") | select -Skip 1 -First 1

if (!$blobObject) {
    Write-Host "##vso[task.complete result=Failed]Could not find blob object with selected version '$($IAIVersion)', exiting..."
}

$baseUrl = $context.BlobEndpoint
$iaiFullUrl = $baseUrl.TrimEnd("/") + "/" + $IAIStorageAccountContainerName + "/" + $blobObject.Name.TrimStart("/")
$fileName = $blobObject.Name.Split("/") | select -Last 1

$iaiLocalFilename = [System.IO.Path]::Combine($IAILocalFolder, $fileName)

Write-Host "##[group]Downloading IAI binaries..."
Write-Host "IAI binary download"
Write-Host "  Version: $($version)"
Write-Host "  Source: $($iaiFullUrl)"
Write-Host "  Target: $($iaiLocalFilename)"

Write-Host "##[command]Downloading binary..."
(New-Object System.Net.WebClient).DownloadFile($iaiFullUrl, $iaiLocalFilename)

Write-Host "Download complete"
Write-Host "##[endgroup]"

Write-Host "Settings Pipelines-Variable 'IAILocalFilename' to $($iaiLocalFilename)..."
Write-Host "##vso[task.setvariable variable=IAILocalFilename;]$($iaiLocalFilename)"