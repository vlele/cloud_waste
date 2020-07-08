# Cost Management

Steps to run the application:
1. Clone the repository and open in Visual Studio.
2. Right click on the Solution, then Click on the "Restore NuGet Packages". Packages will be restored.
3. Right click on the Solution, then Click on the "Build Solution/Rebuild Solution".
4. Click on **Start** to start the Console App.
5. Provide your Username & Password. Application will fetch the subscriptions associated to your user account.
6. Provide the subscription ID for which you want to find the unused Storage Accounts. After providing the Subscription ID, application will fetch all the storage accounts in your subscription.
6. The application will show the following messages for storage accounts based on the conditions:
   - **Warning message**: Unused VHD is present in the storage account.
   - **Information message**: The Storage account does not contains any conatiner.
   - **Information message**: Please enable Diagnostics Settings to check the logs/activity of the storage account.
   - **Information message**: Diagnostics Settings are enabled for this account but logging is still disabled. Please change the Diagnostics Settings.
   - **Information message**: Diagnostics Settings are enabled for this account. Either this storage account have not been used from last 30 days or Logs are not available for this storage account.
   - **Information message**: There are only log entries for GetBlobServiceProperties, ListBlobs, ListContainers from last 30 days for this storage account.
