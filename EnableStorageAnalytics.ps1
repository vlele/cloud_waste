<#
    .SYNOPSIS
        This script is used to enable storage analytics logging on all storage accounts in all subscriptions.
    
	.DESCRIPTION
        The script supports enable storage analytics logging on all storage accounts in all subscriptions.

    .NOTES
        This script is written with Azure PowerShell Az module.
        File Name     : EnableStorageAnalytics.ps1
        Version       : 1.0.0.0

    .EXAMPLE
        Set-AzureSecurityCenterInfo.ps1 -LogRetention 60 
#>

Param(
    [Parameter(HelpMessage = "Log Retention of Storage Analytics in Day",
               Position = 0)]
    [ValidateRange(7,365)]
    [Int]
    $LogRetention = 30
)
try {
	$ErrorActionPreference = "Stop"
	Login-AzAccount
	Get-AzSubscription
	$SubcriptionId = Read-Host "Enter the Subscription ID for which you want to enable analytics for all storage accounts"
	$subscription = Get-AzSubscription -SubscriptionId $SubcriptionId

	if ($subscription -ne "null") {
		$ctx = Set-AzContext -SubscriptionID $SubcriptionId

		Write-Host -ForegroundColor Green "[-] Start checking subscription:" $subscription.Name
		$storageAccounts = Get-AzStorageAccount | Where-Object {$_.Sku.Tier -eq "Standard" }
		foreach ($storageAccount in $storageAccounts) {
			Write-Host -ForegroundColor Yellow "`t [-] Found a storage account named: " $storageAccount.StorageAccountName
			$key = Get-AzStorageAccountKey -ResourceGroupName $storageAccount.ResourceGroupName `
										   -AccountName $storageAccount.StorageAccountName `
										   | Where-Object {$_.KeyName -eq "key1"}
			$ctx = New-AzStorageContext -StorageAccountName $storageAccount.StorageAccountName `
										-StorageAccountKey $key.Value
			$ctx | Set-AzStorageServiceLoggingProperty -ServiceType Blob `
														  -LoggingOperations All `
														  -RetentionDays $LogRetention `
														  -Version "2.0"
			$logging = $ctx | Get-AzStorageServiceLoggingProperty -ServiceType Blob

			if($logging.LoggingOperations -ne "None") {
				Write-Host -ForegroundColor Green "`t [-] Storage Analytics is enabled succesfully in storage account: "$storageAccount.StorageAccountName
			}
			elseif ($logging.LoggingOperations -eq "None") {
				Write-Host -ForegroundColor Red "[!] Storage Analytics is NOT enabled succesfully in storage account: "$storageAccount.StorageAccountName
			}
		}
	}

}
catch
{
	$Error[0]
}

