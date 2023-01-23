#Constants - Names
$dlTempName = 'hoscy-temporary-download'
$dlTempNameZip = $dlTempName + '.zip'
$folderNameConfig = 'config'
$fileNameConfig = 'config.json'
$fileNameExe = 'Hoscy.exe'

#Constants - Other
$thisFile = (Get-Item $PSCommandPath)
$thisFolder = $thisFile.Directory
$deletionWhitelist = @($folderNameConfig, $thisFile.Name)
$ProgressPreference = 'SilentlyContinue'

#Cancelling it not run in "safe" folder
$thisFolderFiles = @(Get-ChildItem -Path $thisFolder.FullName)
Write-Output 'Checking if ran in correct folder'
$hoscyExecutable = $thisFolderFiles | Where-Object { $_.Name -eq $fileNameExe }
if ($hoscyExecutable.Count -eq 0) {
    Write-Error 'Updater is not running in correct folder'
    return
}
Write-Output 'Running in correct folder, updater can continue'

#Cancelling if HOSCY is running
Write-Output 'Checking for running processes labeled "HOSCY"'
$hoscyProcess = Get-Process 'hoscy' -ErrorAction SilentlyContinue
if ($hoscyProcess) {
    Write-Error ('Detected Process "' + $hoscyProcess.Name + '", unable to update')
    return
}
Write-Output 'No processes detected, updater can continue'

#Grabbing download link
Write-Output 'Attempting to find download link on GitHub'
try {
    $gitRequest = Invoke-RestMethod -Uri 'https://api.github.com/repos/pacistardust/hoscy/releases/latest' -UserAgent 'request'
}
catch {
    Write-Error 'Unable to grab download link for latest version'
    return
}
$gitLink = $gitRequest.assets.browser_download_url
Write-Output ('Download link "' + $gitLink + '" has been found')

#Downloading of ZIP
Write-Output ('Starting download of "' + $gitLink + '" from internet')
If (Test-Path $dlTempNameZip) {
    Write-Output('Temporary ZIP detected, deleting before download')
    Remove-Item $dlTempNameZip -Recurse
}
try {
    Invoke-RestMethod -Uri $gitLink -OutFile $dlTempNameZip
}
catch {
    Write-Error 'Unable to download file'
    return
}
Write-Output('Successfully downloaded file from internet')

#Unpacking of ZIP
Write-Output ('Unpacking downloaded file')
If (Test-Path $dlTempName) {
    Write-Output('Temporary folder detected, deleting before unpacking')
    Remove-Item $dlTempName -Recurse
}
try {
    Expand-Archive -Path $dlTempNameZip -DestinationPath $dlTempName
}
catch {}
if (Test-Path $dlTempNameZip) {
    Write-Output('Deleting archive file')
    Remove-Item $dlTempNameZip -Recurse
}
if (!(Test-Path($dlTempName))) {
    Write-Error 'Failed to unpack archive'
    return
}
Write-Output ('Successfully unpacked file')

#Checking if files exist
Write-Output 'Performing sanity checks on file'
$unpackedZipContents = @(Get-ChildItem -Path $dlTempName)
if ($unpackedZipContents.Count -eq 0) {
    Write-Error 'Unpacked file is empty'
    return
}
$unpackedZipContentFolder = $unpackedZipContents[0].FullName
$newVersionFiles = @(Get-ChildItem -Path $unpackedZipContentFolder)
if ($newVersionFiles.Count -eq 0) {
    Write-Error 'Unpacked file is empty'
    return
}
Write-Output 'Sanity checks complete'

#Deleting from HOSCY Folder but config and updater
Write-Output 'Deleting all but and config and updater'
foreach ($thisFolderFile in $thisFolderFiles) {
    if (!$deletionWhitelist.Contains($thisFolderFile.Name)) {
        Remove-Item $thisFolderFile.FullName -Recurse
    }
}
Write-Output('All files deleted')

#Adding everything to HOSCY from temp but updater (and config)
Write-Output 'Moving all files but updater from temp'
foreach ($newVersionFile in $newVersionFiles) {
    if (!$deletionWhitelist.Contains($newVersionFile.Name)) {
        Move-Item -Path $newVersionFile.FullName -Destination $thisFolder.FullName
    }
}
Write-Output('All files moved')

#Deleting temp folder
If (Test-Path $dlTempName) {
    Write-Output('Deleting temporary folder')
    Remove-Item $dlTempName -Recurse
}

#Duplicating config file
$configFolder = $thisFolderFiles | Where-Object { $_.Name -eq $folderNameConfig }
if ($configFolder.Count -ne 0) {
    $configFile = @(Get-ChildItem -Path $configFolder[0].FullName) | Where-Object { $_.Name -eq $fileNameConfig }
    if ($configFile.Count -ne 0) {
        Write-Output('Creating backup of config')
        Copy-Item -Path $configFile[0].FullName -Destination ($configFile[0].FullName + '.backup')
    }
}

Write-Output 'Update complete!'