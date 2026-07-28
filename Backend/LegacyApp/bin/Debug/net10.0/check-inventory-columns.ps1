$ErrorActionPreference = "Stop"
Add-Type -Path ".\Npgsql.dll"

$connString = "Host=192.168.205.23;Port=5432;Database=DCITEST052025;Username=ironlogic;Password=uHEusI9qpB40D!A$"

try {
    $conn = New-Object Npgsql.NpgsqlConnection($connString)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'inventory' ORDER BY ordinal_position;"
    $reader = $cmd.ExecuteReader()

    $columns = @()
    while ($reader.Read()) {
        $colName = $reader.GetString(0)
        $dataType = $reader.GetString(1)
        $columns += "$colName ($dataType)"
    }

    $reader.Close()
    $conn.Close()

    $columns | Out-File "columns.txt" -Encoding utf8
    Write-Host "Columns retrieved successfully."
} catch {
    Write-Host "Error: $_"
}
