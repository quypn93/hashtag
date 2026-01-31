[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$conn = New-Object System.Data.SqlClient.SqlConnection("Server=112.78.2.36;Database=vir62982_tag;User Id=vir62982_user;Password=*1MAbonR?hu7saa7;Encrypt=false;")
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT TOP 10 Id, Title FROM BlogPosts ORDER BY Id DESC"
$reader = $cmd.ExecuteReader()
while($reader.Read()) {
    $id = $reader["Id"]
    $title = $reader["Title"]
    Write-Host "ID: $id | Title: $title"
}
$conn.Close()
