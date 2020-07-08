<#
    .SYNOPSIS
        This script is used to fetch storage metrics "Transactions" on all storage accounts in subscription.
    
	.DESCRIPTION
        The script supports fetching storage metrics "Transactions" on all storage accounts in subscription.

    .NOTES
        This script is written with Azure PowerShell Az module.
        File Name     : GetAzureStorageMetrics.ps1
        Version       : 1.0.0.0

    .EXAMPLE
        GetAzureStorageMetrics.ps1 -DurationToFetchMetrics 30
#>
Param(
    [Parameter(HelpMessage = "Number of days for which we want to fetch the transactions",
               Position = 0)]
    [Int]
    $DurationToFetchMetrics = 30
)
try {
	$ErrorActionPreference = "Stop"
	Login-AzAccount
	Get-AzSubscription
	$SubcriptionId = Read-Host "Enter the Subscription ID for which you want to fetch storage metrics"
	$subscription = Get-AzSubscription -SubscriptionId $SubcriptionId
	$ctx = Set-AzContext -SubscriptionID $SubcriptionId
	Write-Host -ForegroundColor Green "[-] Start checking subscription:" $subscription.Name

	$storageAccounts = Get-AzStorageAccount | Where-Object {$_.Sku.Tier -eq "Standard" }

	$transactionDataArr = @()

	foreach ($storageAccount in $storageAccounts) {
		
		$totalTransactions = 0
		$duration = $DurationToFetchMetrics * (-1)
		$startTime = (Get-Date).ToUniversalTime().AddDays($duration).ToString('yyyy-MM-ddTHH:mm:ssZ')
		$endTime = (Get-date).ToUniversalTime().tostring("yyyy-MM-ddTHH:mm:ssZ")
		$metricData = (Get-AzMetric -ResourceId $storageAccount.Id -MetricName "Transactions" -AggregationType "Total" -StartTime $startTime -EndTime $endTime -TimeGrain "1.00:00:00" -WarningAction:SilentlyContinue).Data
		$metricData | Foreach { $totalTransactions += $_.Total}

		$transactionData = [pscustomobject] @{
			Name = $storageAccount.StorageAccountName
			Transactions = $totalTransactions
			LessTransactions = $totalTransactions -le 10000
		}
		Write-Host "Storage account : " $storageAccount.StorageAccountName
		Write-Host "Transactions : " $totalTransactions
		
		if ($totalTransactions -le 10000) {
			Write-Host -ForegroundColor Yellow $storageAccount.StorageAccountName "has less than 10000 transactions for last" $DurationToFetchMetrics "days. Either the Storage Account is newly created or would you like to check if the Storage Account is in use or not!" 
		}
		Write-Host
		$transactionDataArr += $transactionData
	}
	Write-Host
	Write-Host "Observations for Storage Accounts (Blob Services) for last" $DurationToFetchMetrics "days:"
	$transactionDataArr | Format-Table
}
catch
{
	$Error[0]
}

