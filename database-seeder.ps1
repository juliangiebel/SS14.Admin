# SS14.Admin Database Seeder
# A PowerShell script to create, seed, and manage the SS14 PostgreSQL databases
# Also this could be used for making bascially any ss14 database

param(
    [string]$ConfigPath = "database-seed-config.yml"
)

# Function to parse YAML config file
function Get-YamlConfig {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        Write-Host "Error: Config file not found at $Path" -ForegroundColor Red
        exit 1
    }

    $config = @{}
    $currentSection = $null
    $content = Get-Content $Path

    foreach ($line in $content) {
        $line = $line.Trim()
        if ($line -match '^#' -or $line -eq '') { continue }

        if ($line -match '^(\w+):$') {
            $currentSection = $matches[1]
            $config[$currentSection] = @{}
        }
        elseif ($line -match '^\s*(\w+):\s*(.+)$') {
            $key = $matches[1]
            $value = $matches[2].Trim()
            if ($currentSection) {
                $config[$currentSection][$key] = $value
            }
        }
    }

    return $config
}

# Function to find PostgreSQL installation
function Find-PostgreSQLPath {
    # Common PostgreSQL installation paths
    # if your not using 18 17 16 welp
    $possiblePaths = @(
        "$env:ProgramFiles\PostgreSQL\18\bin",
        "$env:ProgramFiles\PostgreSQL\17\bin",
        "$env:ProgramFiles\PostgreSQL\16\bin",
        "${env:ProgramFiles(x86)}\PostgreSQL\18\bin",
        "${env:ProgramFiles(x86)}\PostgreSQL\17\bin",
        "${env:ProgramFiles(x86)}\PostgreSQL\16\bin"
    )

    foreach ($path in $possiblePaths) {
        if (Test-Path "$path\psql.exe") {
            return $path
        }
    }

    return $null
}

# Function to test PostgreSQL connection
function Test-PostgresConnection {
    param($config)

    try {
        # Try to find PostgreSQL installation
        $pgPath = Find-PostgreSQLPath

        if ($pgPath) {
            Write-Host "Found PostgreSQL at: $pgPath" -ForegroundColor Gray
            $env:PATH = "$pgPath;$env:PATH"
        }
        else {
            # Try to find psql in PATH
            $psqlCmd = Get-Command psql -ErrorAction SilentlyContinue
            if (-not $psqlCmd) {
                Write-Host "ERROR: PostgreSQL (psql) not found!" -ForegroundColor Red
                Write-Host "" -ForegroundColor Yellow
                Write-Host "Please install PostgreSQL from: https://www.postgresql.org/download/" -ForegroundColor Yellow
                Write-Host "Or ensure PostgreSQL bin directory is in your PATH" -ForegroundColor Yellow
                Write-Host "" -ForegroundColor Yellow
                Write-Host "Common installation directories:" -ForegroundColor Yellow
                Write-Host "  - C:\Program Files\PostgreSQL\<version>\bin" -ForegroundColor Gray
                Write-Host "  - C:\Program Files (x86)\PostgreSQL\<version>\bin" -ForegroundColor Gray
                return $false
            }
        }

        # Set password environment variable
        $env:PGPASSWORD = $config.database.password

        # Test connection with psql
        Write-Host "Testing connection to PostgreSQL..." -ForegroundColor Gray
        $result = & psql -h $config.database.host -p $config.database.port -U $config.database.username -d postgres -c "SELECT 1" 2>&1

        if ($LASTEXITCODE -eq 0) {
            return $true
        }

        Write-Host "Error connecting to PostgreSQL. Please ensure PostgreSQL is installed and running." -ForegroundColor Red
        Write-Host "Connection details: Host=$($config.database.host), Port=$($config.database.port), User=$($config.database.username)" -ForegroundColor Yellow
        Write-Host "Error output: $result" -ForegroundColor Yellow
        return $false
    }
    catch {
        Write-Host "Error: $_" -ForegroundColor Red
        Write-Host "Make sure PostgreSQL is installed and the service is running." -ForegroundColor Yellow
        return $false
    }
}

# Function to execute SQL command
function Invoke-SqlCommand {
    param(
        [string]$Command,
        [string]$Database = "postgres",
        $config
    )

    $env:PGPASSWORD = $config.database.password

    $result = & psql -h $config.database.host -p $config.database.port -U $config.database.username -d $Database -c $Command 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "SQL Error: $result" -ForegroundColor Red
        return $false
    }

    return $true
}

# Function to check if database exists
function Test-DatabaseExists {
    param($config)

    $env:PGPASSWORD = $config.database.password
    $result = & psql -h $config.database.host -p $config.database.port -U $config.database.username -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$($config.database.name)'" 2>&1

    return ($result -eq "1")
}

# Function to create database
function New-Database {
    param($config)

    Write-Host "`nCreating database '$($config.database.name)'..." -ForegroundColor Cyan

    if (Test-DatabaseExists -config $config) {
        Write-Host "Database already exists!" -ForegroundColor Yellow
        return $false
    }

    $result = Invoke-SqlCommand -Command "CREATE DATABASE $($config.database.name);" -config $config

    if ($result) {
        Write-Host "Database created successfully!" -ForegroundColor Green
        return $true
    }

    return $false
}

# Function to delete database
function Remove-Database {
    param($config)

    Write-Host "`nDeleting database '$($config.database.name)'..." -ForegroundColor Cyan

    if (-not (Test-DatabaseExists -config $config)) {
        Write-Host "Database does not exist!" -ForegroundColor Yellow
        return $true
    }

    # Terminate active connections
    $terminateCmd = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$($config.database.name)' AND pid <> pg_backend_pid();"
    Invoke-SqlCommand -Command $terminateCmd -config $config | Out-Null

    Start-Sleep -Seconds 1

    $result = Invoke-SqlCommand -Command "DROP DATABASE IF EXISTS $($config.database.name);" -config $config

    if ($result) {
        Write-Host "Database deleted successfully!" -ForegroundColor Green
        return $true
    }

    return $false
}

# Function to run migrations using dotnet ef
function Invoke-Migrations {
    param($config)

    Write-Host "`nRunning EF Core migrations..." -ForegroundColor Cyan

    $projectPath = Join-Path $PSScriptRoot "SS14/Content.Server.Database"

    if (-not (Test-Path $projectPath)) {
        Write-Host "Error: Content.Server.Database project not found at $projectPath" -ForegroundColor Red
        return $false
    }

    Push-Location $projectPath

    try {
        $connString = "Host=$($config.database.host);Port=$($config.database.port);Database=$($config.database.name);Username=$($config.database.username);Password=$($config.database.password)"

        Write-Host "Applying migrations to database..." -ForegroundColor Yellow
        $result = & dotnet ef database update --connection $connString --context PostgresServerDbContext -- --postgresConfiguration true 2>&1

        if ($LASTEXITCODE -eq 0) {
            Write-Host "Migrations applied successfully!" -ForegroundColor Green
            Pop-Location
            return $true
        }
        else {
            Write-Host "Migration error: $result" -ForegroundColor Red
            Pop-Location
            return $false
        }
    }
    catch {
        Write-Host "Error running migrations: $_" -ForegroundColor Red
        Pop-Location
        return $false
    }
}

# Function to generate random data
function Get-RandomName {
    $firstNames = @("John", "Jane", "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry",
                    "Iris", "Jack", "Kate", "Liam", "Mia", "Noah", "Olivia", "Peter", "Quinn", "Rachel",
                    "Sam", "Tina", "Uma", "Victor", "Wendy", "Xavier", "Yara", "Zach", "Aaron", "Bella")

    $lastNames = @("Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
                   "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin",
                   "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson")

    $first = $firstNames | Get-Random
    $last = $lastNames | Get-Random

    return "$first $last"
}

function Get-RandomLoremIpsum {
    param([int]$Words = 10)

    $loremWords = @("lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "sed", "do",
                    "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore", "magna", "aliqua", "enim",
                    "ad", "minim", "veniam", "quis", "nostrud", "exercitation", "ullamco", "laboris", "nisi", "aliquip",
                    "ex", "ea", "commodo", "consequat", "duis", "aute", "irure", "in", "reprehenderit", "voluptate",
                    "velit", "esse", "cillum", "fugiat", "nulla", "pariatur", "excepteur", "sint", "occaecat", "cupidatat")

    $result = @()
    for ($i = 0; $i -lt $Words; $i++) {
        $result += $loremWords | Get-Random
    }

    return ($result -join " ")
}

function Get-RandomGuid {
    return [guid]::NewGuid().ToString()
}

function Get-RandomIPAddress {
    return "$(Get-Random -Min 1 -Max 255).$(Get-Random -Min 0 -Max 255).$(Get-Random -Min 0 -Max 255).$(Get-Random -Min 0 -Max 255)"
}

function Get-RandomDate {
    param(
        [datetime]$Start = (Get-Date).AddYears(-1),
        [datetime]$End = (Get-Date)
    )

    $range = ($End - $Start).TotalSeconds
    $randomSeconds = Get-Random -Min 0 -Max $range

    return $Start.AddSeconds($randomSeconds).ToString("yyyy-MM-dd HH:mm:ss")
}

# Function to seed admin user
function Add-AdminUser {
    param($config)

    Write-Host "`nCreating admin user from config..." -ForegroundColor Cyan

    $adminGuid = $config.admin.guid
    $username = $config.admin.username
    $title = $config.admin.title
    $rankName = $config.admin.rank

    $env:PGPASSWORD = $config.database.password

    # First, let's check the actual column name in the table
    Write-Host "Checking admin_rank table structure..." -ForegroundColor Gray
    $checkColumns = "SELECT column_name FROM information_schema.columns WHERE table_name = 'admin_rank' AND table_schema = 'public';"
    $columns = & psql -h $config.database.host -p $config.database.port -U $config.database.username -d $config.database.name -tAc $checkColumns
    Write-Host "Columns found: $columns" -ForegroundColor Gray

    # Determine the ID column name (could be 'id', 'admin_rank_id', or something else)
    $idColumn = if ($columns -match 'admin_rank_id') { 'admin_rank_id' } else { 'id' }
    Write-Host "Using ID column: $idColumn" -ForegroundColor Gray

    # Check if rank already exists
    $getRankIdSql = "SELECT $idColumn FROM admin_rank WHERE name = '$rankName';"
    $rankId = & psql -h $config.database.host -p $config.database.port -U $config.database.username -d $config.database.name -tAc $getRankIdSql 2>$null

    # If rank doesn't exist, create it
    if ([string]::IsNullOrWhiteSpace($rankId)) {
        Write-Host "Creating new admin rank: $rankName" -ForegroundColor Gray
        $insertRankSql = "INSERT INTO admin_rank (name) VALUES ('$rankName') RETURNING $idColumn;"
        $rankOutput = & psql -h $config.database.host -p $config.database.port -U $config.database.username -d $config.database.name -tAc $insertRankSql 2>&1

        Write-Host "Raw output from INSERT: '$rankOutput'" -ForegroundColor Gray
        Write-Host "Output type: $($rankOutput.GetType().Name)" -ForegroundColor Gray

        # Convert array to string if needed
        $rankOutputStr = if ($rankOutput -is [array]) {
            $rankOutput -join " "
        } else {
            $rankOutput.ToString()
        }

        Write-Host "Output as string: '$rankOutputStr'" -ForegroundColor Gray

        # Extract just the numeric ID from output
        if ($rankOutputStr -match '(\d+)') {
            $rankId = $matches[1]
            Write-Host "Extracted rank ID: $rankId" -ForegroundColor Gray
        }
        else {
            Write-Host "Error: Failed to extract rank ID from output" -ForegroundColor Red
            Write-Host "Raw output: '$rankOutput'" -ForegroundColor Yellow
            return $false
        }

        # Validate we got a valid ID
        if ([string]::IsNullOrWhiteSpace($rankId) -or $rankId -notmatch '^\d+$') {
            Write-Host "Error: Invalid rank ID extracted: '$rankId'" -ForegroundColor Red
            return $false
        }
    }

    # Ensure rank ID is clean
    if ($rankId) {
        $rankId = $rankId.Trim()
    }
    Write-Host "Using admin rank ID: $rankId" -ForegroundColor Gray

    # Create player entry
    $playerSql = @"
INSERT INTO player (user_id, first_seen_time, last_seen_user_name, last_seen_time, last_seen_address)
VALUES ('$adminGuid', NOW(), '$username', NOW(), '127.0.0.1')
ON CONFLICT (user_id) DO NOTHING;
"@

    Invoke-SqlCommand -Command $playerSql -Database $config.database.name -config $config | Out-Null

    # Create admin entry (admin_rank_id can be NULL based on schema)
    $adminSql = @"
INSERT INTO admin (user_id, title, admin_rank_id, deadminned, suspended)
VALUES ('$adminGuid', '$title', $rankId, false, false)
ON CONFLICT (user_id) DO UPDATE SET title = '$title', admin_rank_id = $rankId;
"@

    $result = Invoke-SqlCommand -Command $adminSql -Database $config.database.name -config $config

    if ($result) {
        Write-Host "Admin user created: $username (GUID: $adminGuid)" -ForegroundColor Green

        # Add admin flags (PERMISSIONS and other important flags)
        Write-Host "Adding admin flags..." -ForegroundColor Gray

        $adminFlags = @(
            "PERMISSIONS",  # Can manage other admins
            "BAN",          # Can ban players
            "ADMINHELP",    # Can handle admin help requests
            "ADMIN"         # General admin flag
        )

        foreach ($flag in $adminFlags) {
            $flagSql = @"
INSERT INTO admin_flag (admin_id, flag, negative)
VALUES ('$adminGuid', '$flag', false)
ON CONFLICT DO NOTHING;
"@
            Invoke-SqlCommand -Command $flagSql -Database $config.database.name -config $config | Out-Null
        }

        Write-Host "Admin flags added successfully!" -ForegroundColor Green
        return $true
    }

    return $false
}

# Function to seed random data
function Add-RandomData {
    param($config)

    Write-Host "`nGenerating random data..." -ForegroundColor Cyan

    $env:PGPASSWORD = $config.database.password

    $playerCount = [int]$config.seeding.player_count
    $serverCount = [int]$config.seeding.server_count
    $roundsPerServer = [int]$config.seeding.rounds_per_server

    Write-Host "Creating $playerCount random players..." -ForegroundColor Yellow

    for ($i = 1; $i -le $playerCount; $i++) {
        $guid = Get-RandomGuid
        $name = Get-RandomName
        $ip = Get-RandomIPAddress
        $firstSeen = Get-RandomDate -Start (Get-Date).AddYears(-2) -End (Get-Date).AddMonths(-6)
        $lastSeen = Get-RandomDate -Start (Get-Date).AddMonths(-6) -End (Get-Date)

        $sql = @"
INSERT INTO player (user_id, first_seen_time, last_seen_user_name, last_seen_time, last_seen_address)
VALUES ('$guid', '$firstSeen', '$name', '$lastSeen', '$ip');
"@

        Invoke-SqlCommand -Command $sql -Database $config.database.name -config $config | Out-Null

        if ($i % 10 -eq 0) {
            Write-Host "  Created $i/$playerCount players..." -ForegroundColor Gray
        }
    }

    Write-Host "Creating $serverCount servers with $roundsPerServer rounds each..." -ForegroundColor Yellow

    # Check server table structure for ID column name
    $checkServerColumns = "SELECT column_name FROM information_schema.columns WHERE table_name = 'server' AND table_schema = 'public';"
    $serverColumns = & psql -h $config.database.host -p $config.database.port -U $config.database.username -d $config.database.name -tAc $checkServerColumns
    $serverIdColumn = if ($serverColumns -match 'server_id') { 'server_id' } else { 'id' }

    for ($s = 1; $s -le $serverCount; $s++) {
        $serverName = "Server $s - $(Get-RandomLoremIpsum -Words 2)"

        $sql = "INSERT INTO server (name) VALUES ('$serverName') RETURNING $serverIdColumn;"
        $serverOutput = & psql -h $config.database.host -p $config.database.port -U $config.database.username -d $config.database.name -tAc $sql 2>&1

        # Convert array to string if needed (same as admin_rank handling)
        $serverOutputStr = if ($serverOutput -is [array]) {
            $serverOutput -join " "
        } else {
            $serverOutput.ToString()
        }

        # Extract server ID
        if ($serverOutputStr -match '(\d+)') {
            $serverId = $matches[1]
        } else {
            Write-Host "  Warning: Failed to get server ID for $serverName" -ForegroundColor Yellow
            continue
        }

        for ($r = 1; $r -le $roundsPerServer; $r++) {
            $startDate = Get-RandomDate -Start (Get-Date).AddMonths(-3) -End (Get-Date)

            $roundSql = "INSERT INTO round (server_id, start_date) VALUES ($serverId, '$startDate');"
            Invoke-SqlCommand -Command $roundSql -Database $config.database.name -config $config | Out-Null
        }

        Write-Host "  Created server: $serverName (ID: $serverId)" -ForegroundColor Gray
    }

    Write-Host "Creating connection logs..." -ForegroundColor Yellow
    $connectionCount = [int]$config.seeding.connection_logs_count

    # Get actual server IDs from the database
    $getServerIds = "SELECT $serverIdColumn FROM server;"
    $actualServerIds = & psql -h $config.database.host -p $config.database.port -U $config.database.username -d $config.database.name -tAc $getServerIds

    # Convert to array if needed
    if ($actualServerIds -is [array]) {
        $serverIdsArray = $actualServerIds
    } else {
        $serverIdsArray = @($actualServerIds)
    }

    if ($serverIdsArray.Count -eq 0) {
        Write-Host "  Warning: No servers found, skipping connection logs" -ForegroundColor Yellow
    } else {
        # Get random player GUIDs
        $getPlayers = "SELECT user_id FROM player LIMIT $connectionCount;"
        $playerGuids = & psql -h $config.database.host -p $config.database.port -U $config.database.username -d $config.database.name -tAc $getPlayers

        foreach ($guid in $playerGuids) {
            if ([string]::IsNullOrWhiteSpace($guid)) { continue }

            $time = Get-RandomDate -Start (Get-Date).AddMonths(-6) -End (Get-Date)
            $ip = Get-RandomIPAddress
            # Pick a random server ID from actual servers
            $serverIdRandom = $serverIdsArray | Get-Random
            $trust = [math]::Round((Get-Random -Min 0.0 -Max 1.0), 2)

            $sql = @"
INSERT INTO connection_log (user_id, user_name, time, address, server_id, trust)
VALUES ('$guid', 'Player', '$time', '$ip', $serverIdRandom, $trust);
"@

            Invoke-SqlCommand -Command $sql -Database $config.database.name -config $config | Out-Null
        }
    }

    # Check if admin_notes table exists (might be admin_notes plural)
    $checkAdminNoteTable = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name LIKE 'admin%note%';"
    $adminNoteTableName = & psql -h $config.database.host -p $config.database.port -U $config.database.username -d $config.database.name -tAc $checkAdminNoteTable

    if ([string]::IsNullOrWhiteSpace($adminNoteTableName)) {
        Write-Host "  Warning: No admin notes table found, skipping admin notes" -ForegroundColor Yellow
    } else {
        Write-Host "Creating admin notes (using table: $adminNoteTableName)..." -ForegroundColor Yellow
        $noteCount = [int]$config.seeding.admin_notes_count

        # Get the admin GUID for created_by
        $adminGuid = $config.admin.guid

        # Get some random player GUIDs
        $getNotePlayers = "SELECT user_id FROM player WHERE user_id != '$adminGuid' ORDER BY RANDOM() LIMIT $noteCount;"
        $notePlayerGuids = & psql -h $config.database.host -p $config.database.port -U $config.database.username -d $config.database.name -tAc $getNotePlayers

        foreach ($guid in $notePlayerGuids) {
            if ([string]::IsNullOrWhiteSpace($guid)) { continue }

            $message = Get-RandomLoremIpsum -Words 15
            $createdAt = Get-RandomDate -Start (Get-Date).AddMonths(-6) -End (Get-Date)
            $severity = Get-Random -Min 0 -Max 5

            $sql = @"
INSERT INTO $adminNoteTableName (player_user_id, message, severity, created_by_id, created_at, last_edited_at, playtime_at_note, deleted, secret)
VALUES ('$guid', '$message', $severity, '$adminGuid', '$createdAt', '$createdAt', '01:00:00', false, false);
"@

            Invoke-SqlCommand -Command $sql -Database $config.database.name -config $config | Out-Null
        }
    }

    Write-Host "Random data generated successfully!" -ForegroundColor Green
    return $true
}

# Main menu function
function Show-Menu {
    Clear-Host
    Write-Host "===============================================" -ForegroundColor Cyan
    Write-Host "     SS14.Admin Database Seeding Tool" -ForegroundColor Cyan
    Write-Host "===============================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. Create new empty database" -ForegroundColor White
    Write-Host "2. Create database with random data" -ForegroundColor White
    Write-Host "3. Delete database" -ForegroundColor White
    Write-Host "4. Delete and recreate empty database" -ForegroundColor White
    Write-Host "5. Delete and recreate with random data" -ForegroundColor White
    Write-Host "6. Add admin user only (existing database)" -ForegroundColor White
    Write-Host "7. Add random data only (existing database)" -ForegroundColor White
    Write-Host "8. Test database connection" -ForegroundColor White
    Write-Host "9. Exit" -ForegroundColor White
    Write-Host ""
}

# Main script execution
Write-Host "SS14.Admin Database Seeder" -ForegroundColor Cyan
Write-Host "==========================`n" -ForegroundColor Cyan

# Load configuration
Write-Host "Loading configuration from $ConfigPath..." -ForegroundColor Yellow
$config = Get-YamlConfig -Path $ConfigPath

Write-Host "Configuration loaded successfully!" -ForegroundColor Green
Write-Host "  Database: $($config.database.name)" -ForegroundColor Gray
Write-Host "  Host: $($config.database.host):$($config.database.port)" -ForegroundColor Gray
Write-Host "  Admin User: $($config.admin.username) ($($config.admin.guid))" -ForegroundColor Gray
Write-Host ""

# Main loop
do {
    Show-Menu
    $choice = Read-Host "Please select an option (1-9)"

    switch ($choice) {
        "1" {
            Write-Host "`n--- Create New Empty Database ---" -ForegroundColor Cyan

            if (-not (Test-PostgresConnection -config $config)) {
                Write-Host "Cannot proceed without database connection." -ForegroundColor Red
                Read-Host "Press Enter to continue"
                continue
            }

            if (New-Database -config $config) {
                if (Invoke-Migrations -config $config) {
                    Add-AdminUser -config $config
                }
            }

            Read-Host "`nPress Enter to continue"
        }

        "2" {
            Write-Host "`n--- Create Database with Random Data ---" -ForegroundColor Cyan

            if (-not (Test-PostgresConnection -config $config)) {
                Write-Host "Cannot proceed without database connection." -ForegroundColor Red
                Read-Host "Press Enter to continue"
                continue
            }

            if (New-Database -config $config) {
                if (Invoke-Migrations -config $config) {
                    Add-AdminUser -config $config
                    Add-RandomData -config $config
                }
            }

            Read-Host "`nPress Enter to continue"
        }

        "3" {
            Write-Host "`n--- Delete Database ---" -ForegroundColor Cyan

            if (-not (Test-PostgresConnection -config $config)) {
                Write-Host "Cannot proceed without database connection." -ForegroundColor Red
                Read-Host "Press Enter to continue"
                continue
            }

            $confirm = Read-Host "Are you sure you want to delete the database '$($config.database.name)'? (yes/no)"

            if ($confirm -eq "yes") {
                Remove-Database -config $config
            }
            else {
                Write-Host "Operation cancelled." -ForegroundColor Yellow
            }

            Read-Host "`nPress Enter to continue"
        }

        "4" {
            Write-Host "`n--- Delete and Recreate Empty Database ---" -ForegroundColor Cyan

            if (-not (Test-PostgresConnection -config $config)) {
                Write-Host "Cannot proceed without database connection." -ForegroundColor Red
                Read-Host "Press Enter to continue"
                continue
            }

            $confirm = Read-Host "This will delete and recreate the database '$($config.database.name)'. Continue? (yes/no)"

            if ($confirm -eq "yes") {
                if (Remove-Database -config $config) {
                    if (New-Database -config $config) {
                        if (Invoke-Migrations -config $config) {
                            Add-AdminUser -config $config
                        }
                    }
                }
            }
            else {
                Write-Host "Operation cancelled." -ForegroundColor Yellow
            }

            Read-Host "`nPress Enter to continue"
        }

        "5" {
            Write-Host "`n--- Delete and Recreate with Random Data ---" -ForegroundColor Cyan

            if (-not (Test-PostgresConnection -config $config)) {
                Write-Host "Cannot proceed without database connection." -ForegroundColor Red
                Read-Host "Press Enter to continue"
                continue
            }

            $confirm = Read-Host "This will delete and recreate the database '$($config.database.name)' with random data. Continue? (yes/no)"

            if ($confirm -eq "yes") {
                if (Remove-Database -config $config) {
                    if (New-Database -config $config) {
                        if (Invoke-Migrations -config $config) {
                            Add-AdminUser -config $config
                            Add-RandomData -config $config
                        }
                    }
                }
            }
            else {
                Write-Host "Operation cancelled." -ForegroundColor Yellow
            }

            Read-Host "`nPress Enter to continue"
        }

        "6" {
            Write-Host "`n--- Add Admin User Only ---" -ForegroundColor Cyan

            if (-not (Test-PostgresConnection -config $config)) {
                Write-Host "Cannot proceed without database connection." -ForegroundColor Red
                Read-Host "Press Enter to continue"
                continue
            }

            if (-not (Test-DatabaseExists -config $config)) {
                Write-Host "Database does not exist. Please create it first." -ForegroundColor Red
                Read-Host "Press Enter to continue"
                continue
            }

            Add-AdminUser -config $config

            Read-Host "`nPress Enter to continue"
        }

        "7" {
            Write-Host "`n--- Add Random Data Only ---" -ForegroundColor Cyan

            if (-not (Test-PostgresConnection -config $config)) {
                Write-Host "Cannot proceed without database connection." -ForegroundColor Red
                Read-Host "Press Enter to continue"
                continue
            }

            if (-not (Test-DatabaseExists -config $config)) {
                Write-Host "Database does not exist. Please create it first." -ForegroundColor Red
                Read-Host "Press Enter to continue"
                continue
            }

            Add-RandomData -config $config

            Read-Host "`nPress Enter to continue"
        }

        "8" {
            Write-Host "`n--- Test Database Connection ---" -ForegroundColor Cyan

            if (Test-PostgresConnection -config $config) {
                Write-Host "Connection successful!" -ForegroundColor Green

                if (Test-DatabaseExists -config $config) {
                    Write-Host "Database '$($config.database.name)' exists." -ForegroundColor Green
                }
                else {
                    Write-Host "Database '$($config.database.name)' does not exist." -ForegroundColor Yellow
                }
            }
            else {
                Write-Host "Connection failed!" -ForegroundColor Red
            }

            Read-Host "`nPress Enter to continue"
        }

        "9" {
            Write-Host "`nExiting..." -ForegroundColor Cyan
            return
        }

        default {
            Write-Host "`nInvalid option. Please select 1-9." -ForegroundColor Red
            Start-Sleep -Seconds 2
        }
    }
} while ($true)
