[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = "1.0.0",
    [switch]$SkipInstaller,
    [switch]$Upload,
    [switch]$PublishSource,
    [string]$DeployConfigPath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root "NNPA.sln"
$projectDir = Join-Path $root "NNPA"
$assemblyInfoPath = Join-Path $projectDir "Properties\AssemblyInfo.cs"
$outputDir = Join-Path $projectDir "bin\Release"
$artifactDir = Join-Path $root "artifacts\release"
$stagingDir = Join-Path $artifactDir ("NNPA-Media-{0}" -f $Version)
$installerName = "NNPA-Media-Setup-{0}.exe" -f $Version
$installerPath = Join-Path $artifactDir $installerName
$installerScript = Join-Path $root "installer\NNPA-Media.iss"
$manifestPath = Join-Path $artifactDir "version.json"
if ([string]::IsNullOrWhiteSpace($DeployConfigPath)) {
    $DeployConfigPath = Join-Path $root "deployment\vps-deploy-config.local.json"
}
elseif (-not [IO.Path]::IsPathRooted($DeployConfigPath)) {
    $DeployConfigPath = Join-Path $root $DeployConfigPath
}

function Find-MSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $candidate = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
        if ($candidate) { return $candidate }
    }
    $candidates = @(
        (Join-Path $env:ProgramFiles "Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe")
    )
    return $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

function Find-InnoSetup {
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    )
    return $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

function Update-AssemblyVersion {
    $content = [IO.File]::ReadAllText($assemblyInfoPath)
    $rules = @(
        @('(?m)^\[assembly: AssemblyVersion\("[^"]+"\)\]\r?$', "[assembly: AssemblyVersion(`"$Version.0`")]") ,
        @('(?m)^\[assembly: AssemblyFileVersion\("[^"]+"\)\]\r?$', "[assembly: AssemblyFileVersion(`"$Version.0`")]") ,
        @('(?m)^\[assembly: AssemblyInformationalVersion\("[^"]+"\)\]\r?$', "[assembly: AssemblyInformationalVersion(`"$Version`")]")
    )
    foreach ($rule in $rules) {
        if ([regex]::Matches($content, $rule[0]).Count -ne 1) { throw "Không tìm thấy duy nhất một thuộc tính phiên bản trong AssemblyInfo.cs." }
        $content = [regex]::Replace($content, $rule[0], $rule[1])
    }
    [IO.File]::WriteAllText($assemblyInfoPath, $content, (New-Object Text.UTF8Encoding($true)))
}

function Read-DeployConfig {
    if (-not (Test-Path $DeployConfigPath)) {
        throw "Không tìm thấy cấu hình triển khai local: $DeployConfigPath. Hãy sao chép deployment\vps-deploy-config.example.json thành deployment\vps-deploy-config.local.json và điền thông tin thật."
    }

    $config = Get-Content $DeployConfigPath -Raw | ConvertFrom-Json
    foreach ($name in @('vpsHost', 'vpsPort', 'vpsUser', 'remoteDownloadDir', 'publicBaseUrl')) {
        if ($null -eq $config.$name -or [string]::IsNullOrWhiteSpace([string]$config.$name)) {
            throw "Thiếu thuộc tính '$name' trong cấu hình triển khai."
        }
    }
    if ([int]$config.vpsPort -lt 1 -or [int]$config.vpsPort -gt 65535) { throw "vpsPort không hợp lệ." }
    $uri = $null
    if (-not [Uri]::TryCreate([string]$config.publicBaseUrl, [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -notin @('http', 'https')) {
        throw "publicBaseUrl phải là URL HTTP hoặc HTTPS tuyệt đối."
    }
    return $config
}

function Publish-Release {
    param($Config)

    $ssh = (Get-Command ssh.exe -ErrorAction SilentlyContinue).Source
    $scp = (Get-Command scp.exe -ErrorAction SilentlyContinue).Source
    if (-not $ssh -or -not $scp) { throw "Không tìm thấy ssh.exe/scp.exe của OpenSSH." }

    $port = [string][int]$Config.vpsPort
    $target = "{0}@{1}" -f $Config.vpsUser, $Config.vpsHost
    $remoteDir = ([string]$Config.remoteDownloadDir).TrimEnd('/')
    if ($remoteDir -notmatch '^/[A-Za-z0-9._/-]+$') { throw "remoteDownloadDir chứa ký tự không được hỗ trợ." }

    $sshArgs = @('-p', $port)
    $scpArgs = @('-P', $port)
    if ($Config.PSObject.Properties.Name -contains 'sshKeyPath' -and -not [string]::IsNullOrWhiteSpace([string]$Config.sshKeyPath)) {
        $keyPath = [Environment]::ExpandEnvironmentVariables([string]$Config.sshKeyPath)
        if (-not [IO.Path]::IsPathRooted($keyPath)) { $keyPath = Join-Path $root $keyPath }
        if (-not (Test-Path $keyPath)) { throw "Không tìm thấy SSH key: $keyPath" }
        $sshArgs += @('-i', $keyPath)
        $scpArgs += @('-i', $keyPath)
    }

    Write-Host "Ensuring remote release directory exists..."
    & $ssh @sshArgs $target "mkdir -p '$remoteDir'"
    if ($LASTEXITCODE -ne 0) { throw "Không thể tạo hoặc truy cập thư mục phát hành trên VPS." }

    Write-Host "Uploading installer first..."
    & $scp @scpArgs $installerPath ("{0}:{1}/{2}" -f $target, $remoteDir, $installerName)
    if ($LASTEXITCODE -ne 0) { throw "Upload installer thất bại." }

    Write-Host "Uploading version.json last..."
    & $scp @scpArgs $manifestPath ("{0}:{1}/version.json" -f $target, $remoteDir)
    if ($LASTEXITCODE -ne 0) { throw "Upload version.json thất bại." }

    Write-Host "Đã upload bản phát hành theo thứ tự an toàn."
}

function Publish-Source {
    $git = (Get-Command git.exe -ErrorAction SilentlyContinue).Source
    if (-not $git) { throw "Không tìm thấy git.exe." }

    & $git -C $root rev-parse --is-inside-work-tree | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Thư mục dự án không phải Git repository." }

    & $git -C $root add --all
    if ($LASTEXITCODE -ne 0) { throw "Không thể stage thay đổi Git." }

    $stagedFiles = & $git -C $root diff --cached --name-only
    if ($LASTEXITCODE -ne 0) { throw "Không thể kiểm tra thay đổi Git." }
    if ($stagedFiles) {
        & $git -C $root commit -m ("Release NNPA Media v{0}" -f $Version)
        if ($LASTEXITCODE -ne 0) { throw "Git commit thất bại." }
    }
    else {
        Write-Host "Không có thay đổi source mới để commit."
    }

    & $git -C $root push origin HEAD
    if ($LASTEXITCODE -ne 0) { throw "Git push thất bại." }
    Write-Host "Đã commit (nếu có thay đổi) và push source lên origin."
}

$msbuild = Find-MSBuild
if (-not $msbuild) { throw "Không tìm thấy MSBuild của Visual Studio 2022." }
if ($SkipInstaller -and $Upload) { throw "Không thể dùng đồng thời -SkipInstaller và -Upload." }
if ($SkipInstaller -and $PublishSource) { throw "Không thể dùng đồng thời -SkipInstaller và -PublishSource." }

$config = $null
if (-not $SkipInstaller) { $config = Read-DeployConfig }

Update-AssemblyVersion
New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null
if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }

Write-Host "Building NNPA Media $Version (Release)..."
& $msbuild $solution /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" /m
if ($LASTEXITCODE -ne 0) { throw "MSBuild thất bại với mã $LASTEXITCODE." }

Copy-Item $outputDir $stagingDir -Recurse -Force

if (-not $SkipInstaller) {
    $iscc = Find-InnoSetup
    if (-not $iscc) { throw "Không tìm thấy Inno Setup 6 (ISCC.exe)." }
    if (Test-Path $installerPath) { Remove-Item $installerPath -Force }
    Write-Host "Building installer..."
    & $iscc "/DAppVersion=$Version" "/DSourceDir=$stagingDir" "/DOutputDir=$artifactDir" $installerScript
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $installerPath)) { throw "Build installer thất bại." }

    $baseUrl = ([string]$config.publicBaseUrl).TrimEnd('/')
    $hash = (Get-FileHash $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $manifest = [ordered]@{
        version     = $Version
        releaseDate = (Get-Date).ToString('yyyy-MM-dd')
        downloadUrl = "$baseUrl/$installerName"
        title       = "Bản cập nhật NNPA Media v$Version"
        isMandatory = $false
        changelog   = @("Bổ sung kiểm tra và cập nhật phần mềm trực tuyến", "Bổ sung bộ cài đặt NNPA Media")
        sha256      = $hash
    }
    [IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 4), (New-Object Text.UTF8Encoding($false)))
    Write-Host "Installer: $installerPath"
    Write-Host "Manifest:  $manifestPath"
    Write-Host "SHA-256:   $hash"

    if ($Upload) { Publish-Release -Config $config }
}

if ($PublishSource) { Publish-Source }

if ($Upload -and $PublishSource) {
    Write-Host "Hoàn tất build, upload VPS, commit và push source."
}
elseif ($Upload) {
    Write-Host "Hoàn tất build và upload bản phát hành."
}
else {
    Write-Host "Hoàn tất bản phát hành local; chưa upload."
}
