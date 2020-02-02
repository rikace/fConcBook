IF NOT EXIST paket\paket.exe (
    START /WAIT .paket/paket.exe install
)
.paket\paket.exe restore
dotnet build --no-restore
dotnet test --no-restore
