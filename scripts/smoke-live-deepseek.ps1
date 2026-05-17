param(
    [string]$ApiKey = $(if ($env:DeepSeek__ApiKey) { $env:DeepSeek__ApiKey } else { $env:DEEPSEEK_API_KEY }),
    [string]$BaseUrl = "http://127.0.0.1:5099",
    [string]$Plan = "Premium"
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    throw "DeepSeek API key is required. Pass -ApiKey or set DeepSeek__ApiKey/DEEPSEEK_API_KEY."
}

$workspace = Split-Path -Parent $PSScriptRoot
$job = Start-Job -ScriptBlock {
    param($workdir, $apiKey, $baseUrl)
    Set-Location $workdir
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:DOTNET_CLI_HOME = $workdir
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
    $env:DeepSeek__ApiKey = $apiKey
    & "C:\Program Files\dotnet\dotnet.exe" run --no-build --project src/RentGen.Api/RentGen.Api.csproj --urls $baseUrl
} -ArgumentList $workspace, $ApiKey, $BaseUrl

try {
    $ready = $false
    for ($i = 0; $i -lt 120; $i++) {
        try {
            $null = Invoke-WebRequest -Uri "$BaseUrl/swagger/index.html" -UseBasicParsing -TimeoutSec 2
            $ready = $true
            break
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    if (-not $ready) {
        throw "API did not start in time."
    }

    $headers = @{
        "Content-Type" = "application/json"
        "X-Plan" = $Plan
    }

    $draft = Invoke-RestMethod -Uri "$BaseUrl/api/drafts" -Method Post -Headers $headers -Body (@{
        documentType = 1
    } | ConvertTo-Json)

    $answers = @(
        @{ stepKey = "landlord_type"; value = "individual" },
        @{ stepKey = "landlord_name"; value = "Ivan Ivanov" },
        @{ stepKey = "landlord_phone"; value = "+79000000001" },
        @{ stepKey = "tenant_type"; value = "individual" },
        @{ stepKey = "tenant_name"; value = "Petr Petrov" },
        @{ stepKey = "tenant_phone"; value = "+79000000002" },
        @{ stepKey = "property_type"; value = "apartment" },
        @{ stepKey = "property_address"; value = "Moscow, Example Street 1-10" },
        @{ stepKey = "property_description"; value = "One-bedroom furnished apartment." },
        @{ stepKey = "property_area_sqm"; value = 42 },
        @{ stepKey = "lease_start_date"; value = "2026-04-01" },
        @{ stepKey = "lease_end_date"; value = "2027-03-31" },
        @{ stepKey = "rent_amount"; value = 50000 },
        @{ stepKey = "payment_due_day"; value = 5 },
        @{ stepKey = "payment_method"; value = "bank_transfer" },
        @{ stepKey = "utilities_payment_terms"; value = "Tenant pays utilities according to invoices and meter readings." },
        @{ stepKey = "deposit_required"; value = $true },
        @{ stepKey = "deposit_amount"; value = 50000 },
        @{ stepKey = "deposit_return_terms"; value = "Returned after inspection if there are no debts or damages." },
        @{ stepKey = "has_pets_clause"; value = $true },
        @{ stepKey = "pets_details"; value = "One indoor cat. The tenant is responsible for any pet-related damage." },
        @{ stepKey = "pet_type"; value = "cat" },
        @{ stepKey = "pet_count"; value = 1 },
        @{ stepKey = "pet_residence_rules"; value = "No breeding. The tenant pays for cleaning and repairs caused by the pet." },
        @{ stepKey = "sublease_allowed"; value = $false },
        @{ stepKey = "additional_rules"; value = "No smoking indoors. Keep quiet after 22:00." },
        @{ stepKey = "handover_transfer_date"; value = "2026-04-01" },
        @{ stepKey = "handover_property_condition"; value = "Clean, habitable, and ready for move-in." },
        @{ stepKey = "handover_visible_defects"; value = "No visible defects." },
        @{ stepKey = "handover_keys_transferred"; value = "2 apartment keys and 1 building access chip." },
        @{ stepKey = "handover_meter_readings"; value = "Electricity 15420 kWh, cold water 124 m3." }
    )

    foreach ($answer in $answers) {
        $null = Invoke-RestMethod -Uri "$BaseUrl/api/drafts/$($draft.draftId)/answers" -Method Post -Headers $headers -Body ($answer | ConvertTo-Json)
    }

    $help = Invoke-RestMethod -Uri "$BaseUrl/api/drafts/$($draft.draftId)/ask" -Method Post -Headers $headers -Body (@{
        question = "Explain in simple terms how the security deposit usually works."
        stepKey = "deposit_required"
    } | ConvertTo-Json)

    $generation = Invoke-RestMethod -Uri "$BaseUrl/api/drafts/$($draft.draftId)/generate" -Method Post -Headers $headers -Body (@{
        includeGuide = $true
        requestedAppendices = @()
    } | ConvertTo-Json -Depth 5)

    $document = Invoke-RestMethod -Uri "$BaseUrl/api/documents/$($generation.documentId)" -Method Get -Headers $headers
    $guide = Invoke-RestMethod -Uri "$BaseUrl/api/documents/$($generation.documentId)/guide" -Method Get -Headers $headers
    $appendix = Invoke-RestMethod -Uri "$BaseUrl/api/documents/$($generation.documentId)/appendices" -Method Post -Headers $headers -Body (@{
        appendixType = 1
    } | ConvertTo-Json)
    $appendixDetails = Invoke-RestMethod -Uri "$BaseUrl/api/appendices/$($appendix.appendixId)" -Method Get -Headers $headers

    [pscustomobject]@{
        DraftId = $draft.draftId
        DocumentId = $generation.documentId
        AppendixId = $appendix.appendixId
        Plan = $Plan
        HelpAnswerPreview = if ($help.answer.Length -gt 220) { $help.answer.Substring(0, 220) } else { $help.answer }
        DocumentPreview = if ($document.content.Length -gt 320) { $document.content.Substring(0, 320) } else { $document.content }
        GuidePreview = if ($guide.content.Length -gt 220) { $guide.content.Substring(0, 220) } else { $guide.content }
        AppendixPreview = if ($appendixDetails.content.Length -gt 220) { $appendixDetails.content.Substring(0, 220) } else { $appendixDetails.content }
        GuideType = $guide.guideType
    } | ConvertTo-Json -Depth 5
}
finally {
    Stop-Job $job -ErrorAction SilentlyContinue | Out-Null
    Remove-Job $job -Force -ErrorAction SilentlyContinue | Out-Null
}
