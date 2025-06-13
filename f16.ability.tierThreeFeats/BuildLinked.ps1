# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/f16.ability.tierThreeFeats/*" -Force -Recurse
dotnet publish "./f16.ability.tierThreeFeats.csproj" -c Release -o "$env:RELOADEDIIMODS/f16.ability.tierThreeFeats" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location