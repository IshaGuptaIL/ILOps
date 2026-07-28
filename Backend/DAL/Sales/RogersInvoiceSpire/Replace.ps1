$path = 'c:\Users\DELL\Downloads\My Code\Backend\DAL\Sales\RogersInvoiceSpire\RogersInvoiceSpireDA.cs'
$content = Get-Content -Path $path -Raw
$content = [regex]::Replace($content, 'Convert\.ToDouble\(reader\.GetValue\((\d+)\)\)', 'SafeGetDouble(reader.GetValue($1))')
$content = [regex]::Replace($content, 'Convert\.ToInt32\(reader\.GetValue\((\d+)\)\)', 'SafeGetInt(reader.GetValue($1))')
$content = [regex]::Replace($content, 'reader\.GetString\((\d+)\)', 'SafeGetString(reader.GetValue($1))')
$content = [regex]::Replace($content, 'reader\.GetDateTime\((\d+)\)', 'SafeGetDateTime(reader.GetValue($1))')
$content = [regex]::Replace($content, 'Convert\.ToDecimal\(reader\.GetValue\((\d+)\)\)', 'SafeGetDecimal(reader.GetValue($1))')
Set-Content -Path $path -Value $content
Write-Output 'Done'
