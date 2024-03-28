function Exit-Script {

    param (
        [Parameter(Mandatory)]
        [string]
        $ErrorMessage
    )

    Write-Host -ForegroundColor Red "ERROR:"$ErrorMessage

    Exit
}

$ErrorActionPreference = 'Stop'

# Get ffmpeg path and test if the file exists
$ffmpeg = "ffmpeg\ffmpeg.exe"

if (-not (Test-Path $ffmpeg)) {
    Exit-Script "FFMPEG was not found in .\ffmpeg\"
}

# Get video path from args and test if it's correct
$discussionPath = $args[0]

if ($null -eq $discussionPath) {
    Exit-Script "Discussion path wasn't specified"
}

if (-not (Test-Path $discussionPath)) {
    Exit-Script "Discussion path is incorrect"    
}

# Add \ to the path in case it's not there
if ($discussionPath[$discussionPath.Count - 1] -ne "\") {
    $discussionPath += "\"
}

$videosPath = $args[1]

if ($null -eq $videosPath) {
    Exit-Script "Videos path wasn't specified"
}

if (-not (Test-Path $videosPath)) {
    Exit-Script "Videos path is incorrect"
}

# Add \ to the path in case it's not there
if ($videosPath[$videosPath.Count - 1] -ne "\") {
    $videosPath += "\"
}

$discussionId = $args[2]

if ($null -eq $discussionId) {
    Exit-Script "Discussion Id wasn't specified"
}

$videoSpeed = $args[3]

if ($null -eq $videoSpeed) {
    Exit-Script "Video Speed wasn't specified"
}

# Checks if $videoSpeed is float
if (-not ($videoSpeed -match "^[\d\.]+$") -or $videoSpeed -eq 0) {
    Exit-Script "Incorrect Video Speed"
}

$videoLength = 1 / $videoSpeed

# Get all files from the video path and check if they are correct
# The number of .png .wav .txt must match and there has to be exactly the same number of each file type in the folder
# Entries are counted from 1 upwards, there can't be any mismatch in names
$files = Get-ChildItem $discussionPath | Where-Object {$_.Name -like "*.png" -or
                                          $_.Name -like "*.wav*" -or
                                          $_.Name -like "*.txt*"}

if (!(($files | Where-Object {$_.name -like "*.png"}).Count -eq
    ($files | Where-Object {$_.name -like "*.wav"}).Count -eq
    ($files | Where-Object {$_.name -like "*.txt"}).Count)) {
        Exit-Script "Incorrect number of files"
    }

$fileGroups = ((($files.name -replace ".png","") -replace ".wav","") -replace ".txt","") | Group-Object 

if (($files.Count / $fileGroups.Count) -ne 3 -or $files.Count % 3 -ne 0) {
    Exit-Script "Incorrect filenames"
}

for ($i = 0; $i -lt $fileGroups.Values.Count; ++$i) {
    if ($fileGroups[$i].Name -ne ($i + 1)) {
        Exit-Script ("Incorrect numbers in filenames - " + ($i + 1) + " was expected")
    }
}

# Create a list of entries, each containing paths to files and lengths of the entry's video
$entries = @()

for ($i = 0; $i -lt $fileGroups.Values.Count; ++$i) {
    $index = $i + 1
    try {
        $length = Get-Content ($discussionPath + "$index.txt")
    }
    catch {
        Exit-Script "Can't read audio length, incorrect file path - $_"
    }

    try {
        $length = [float] $length
    }
    catch {
        Exit-Script "Reading audio lengths - $_"
    }

    $index = $index.ToString()
    $entries += [PSCustomObject]@{
        Screenshot = $discussionPath + $index + ".png"
        Audio = $discussionPath + $index + ".wav"
        Length = $length
        Video = $discussionPath + $index + ".mp4"
    }
}

# Create videos for each entry and create a temporary batch script which contains a command that combines all videos into one
# The batch file is needed because there were problems when running merging command from pwsh
$ffmpegCommandListOfEntries = ""
$ffmpegCommandFilter = ""

$completeVideoPath = $videosPath + $discussionId + ".mp4"
$videoMergeScriptPath = $discussionPath + "MergeVideos.bat"

for ($i = 0; $i -lt $entries.Count; ++$i) {
    & $ffmpeg -y -loop 1 -i $entries[$i].Screenshot -i $entries[$i].Audio -tune stillimage -c:a aac -b:a 192k -pix_fmt yuv420p -vf scale=1080:1920 -t $entries[$i].Length $entries[$i].Video

    $ffmpegCommandListOfEntries += "-i `"" + $entries[$i].Video + "`" "
    $ffmpegCommandFilter += "[$i" + ":v" + "][$i" + ":a]"
}

$ffmpegCommandFilter += "concat=n=$($entries.Count):v=1:a=1[v][a]"

$command = "`"$ffmpeg`" -y $ffmpegCommandListOfEntries -filter_complex `"$ffmpegCommandFilter`" -map `"[v]`" -map `"[a]`" `"video.mp4`""
$commandSpeedUp = "`"$ffmpeg`" -i `"video.mp4`" -vf `"setpts=$videoLength*PTS`" -filter:a `"atempo=$videoSpeed`" `"$completeVideoPath`""

Set-Content -Path $videoMergeScriptPath -Value ($command + "`n" + $commandSpeedUp)
& $videoMergeScriptPath
Remove-Item $videoMergeScriptPath







        
