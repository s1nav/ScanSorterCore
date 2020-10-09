# Manage-ScanSorterService.ps1

$ServiceName = "ScanSorterCore"
$ServiceDisplayName = "Scan Sorter"
$ServiceDescription = "Sorting scan files to users home directory"
$Cancel = $false

if (-Not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')) {
    if ([int](Get-CimInstance -Class Win32_OperatingSystem | Select-Object -ExpandProperty BuildNumber) -ge 6000) {
        $CommandLine = "-File `"" + $MyInvocation.MyCommand.Path + "`" " + $MyInvocation.UnboundArguments
        Start-Process -FilePath PowerShell.exe -Verb Runas -ArgumentList $CommandLine
        Exit
    }
}

function Install-Sorter {
    $BinaryPath = $PSScriptRoot + "\WorkerService.exe"
    if(Test-Path $BinaryPath) {
        New-Service -Name $ServiceName -BinaryPathName $BinaryPath -StartupType Automatic -DisplayName $ServiceDisplayName -Description $ServiceDescription -Credential (Get-Credential)
    }
    else {
        Write-Host "Could not find binary" -ForegroundColor Red
    }
}

do {
    $Service = Get-Service $ServiceName -ErrorAction SilentlyContinue
    if ($Service) {
        $JsonPath = $PSScriptRoot + "\appsettings.json"
        Write-Host "======================"
        Write-Host "Scan Sorter management"
        Write-Host "[1] - Show service information"
        Write-Host "[2] - Start service"
        Write-Host "[3] - Stop service"
        Write-Host "[4] - Restart service"
        Write-Host "[5] - Uninstall service"
        Write-Host "[6] - Show parameters"
        Write-Host "[7] - Edit parameters"
        Write-Host "[q] - Quit"
        Write-Host "======================"
        
        $Selection = Read-Host "Choose option"

        switch ($Selection) {
            '1' {
                Write-Host "Service information:"
            }
            '2' {
                Start-Service $ServiceName
            }
            '3' {
                Stop-Service $ServiceName
            }
            '4' {
                Stop-Service $ServiceName
                Start-Service $ServiceName
            }
            '5' {
                Stop-Service $ServiceName
                if ($PSVersionTable.PSVersion.Major -ge 6) {
                    Remove-Service $ServiceName 
                }
                else {
                    $SvcName = "'" + $ServiceName + "'"
                    (Get-WmiObject -Class Win32_Service -Filter "Name=$SvcName").delete()
                }
            }
            '6' {
                if(Test-Path $JsonPath) {
                    (Get-Content $JsonPath -Raw | ConvertFrom-Json).Settings
                }
            }
            '7' {
                Start-Process -FilePath $JsonPath
            }
            'q' {
                $Cancel = $true
            }
        }
        $Service = Get-Service $ServiceName -ErrorAction SilentlyContinue
        Write-Host "INSTALLED" -ForegroundColor Green
        if ($Service.Status -eq "Running") {
            Write-Host "Service name:`t" $Service.Name -ForegroundColor Green
            Write-Host "Service status:`t" $Service.Status -ForegroundColor Green
        }
        else {
            Write-Host "Service name:`t" $Service.Name -ForegroundColor Red
            Write-Host "Service status:`t" $Service.Status -ForegroundColor Red
        }
    }
    else {
        Write-Host $ServiceName " not installed" -ForegroundColor Red
        $Selection = Read-Host "Do you whant to install Scan Sorter Core service? (y/n)"
        switch ($Selection) {
            'y' {
                Install-Sorter
            }
            'n' {
                $Cancel = $true
            }
        }
    }
    Write-Host ""
} until ($Cancel -eq $true)


