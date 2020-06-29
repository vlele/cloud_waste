# Cost Management

Step to execute the code:
1. Clone the repository and open in Visual Studio.
2. Right click on the Solution, Click on the "Restore NuGet Packages". After the packages are restored.
3. Right click on the Solution, Click on the "Build Solution".
4. Click on Start to start the Console App.
5. Provide your Username & Password. Application will fetch the subscriptions, resource groups and storage accounts in your subscription.
6. The application will show the following messages based on the conditions:
   - **Warning message**: Unused VHD is present in the storage account.
   - **Information message**: The Storage account does not contains any conatiner.
   - **Information message**: Please enable Diagnostics Settings to check the logs/activity of the storage account.
   - **Information message**: Diagnostics Settings are enabled for this account but logging is still disabled. Please change the Diagnostics Settings.
