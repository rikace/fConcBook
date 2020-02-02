#!/bin/sh
if [ ! -e ".paket/paket" ]
then
    dotnet tool install paket --tool-path .paket
fi
.paket/paket restore
dotnet restore
dotnet build --no-restore
dotnet test --no-restore
