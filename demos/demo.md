# Call API Management with Managed Identity - Demo

This demo shows how to call Azure API Management (APIM) from Azure Functions and Logic Apps using managed identity authentication with OAuth. It also shows how one APIM API can securely call another APIM API using its managed identity, enabling secret-free, secure authentication between Azure services.

The template deploys an Azure API Management service with two APIs: a protected backend API secured with OAuth and an unprotected public API that calls the protected API using API Management's system-assigned managed identity. It also deploys a .NET 9 Azure Function App and an Azure Logic App (Standard), both configured to call the protected API using their own system-assigned managed identities. Supporting resources such as Application Insights, a Log Analytics workspace, a Storage Account and Entra ID app registration are also provisioned. See the following diagram for an overview:

![Overview](https://raw.githubusercontent.com/ronaldbosma/call-apim-with-managed-identity/refs/heads/main/images/diagrams-overview.png)

## 1. What resources get deployed

The following resources are deployed in a resource group in your Azure subscription:

![Deployed Resources](https://raw.githubusercontent.com/ronaldbosma/call-apim-with-managed-identity/refs/heads/main/images/deployed-resources.png)

The following app registration is created in your Entra ID tenant:

![Deployed App Registrations](https://raw.githubusercontent.com/ronaldbosma/call-apim-with-managed-identity/refs/heads/main/images/deployed-app-registrations.png)

The deployed resources follow the naming convention: `<resource-type>-<environment-name>-<region>-<instance>`.


## 2. What you can demo after deployment