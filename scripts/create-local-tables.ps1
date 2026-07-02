# Creates all DynamoDB tables on DynamoDB Local (localhost:8000)
$endpoint = "http://localhost:8100"

$tables = @(
    @{ Name = "Users"; PK = "userId" },
    @{ Name = "Mentors"; PK = "mentorId" },
    @{ Name = "Mentees"; PK = "menteeId" },
    @{ Name = "Sessions"; PK = "sessionId" },
    @{ Name = "Notifications"; PK = "notificationId" },
    @{ Name = "Jobs"; PK = "postingId" },
    @{ Name = "Meetups"; PK = "meetupId" },
    @{ Name = "EngagementEvents"; PK = "eventId" }
)

foreach ($table in $tables) {
    Write-Host "Creating table: $($table.Name)..." -ForegroundColor Cyan
    aws dynamodb create-table `
        --endpoint-url $endpoint `
        --table-name $table.Name `
        --attribute-definitions "AttributeName=$($table.PK),AttributeType=S" `
        --key-schema "AttributeName=$($table.PK),KeyType=HASH" `
        --billing-mode PAY_PER_REQUEST `
        --no-cli-pager 2>$null

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Created: $($table.Name)" -ForegroundColor Green
    } else {
        Write-Host "  Already exists: $($table.Name)" -ForegroundColor Yellow
    }
}

Write-Host "`nAll tables ready." -ForegroundColor Green
